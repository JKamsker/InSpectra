using System.Text.Json.Nodes;
using InSpectra.Lib.Contracts;
using InSpectra.Lib;
using InSpectra.Lib.Contracts.Providers;
using InSpectra.Lib.Modes.CliFx.Execution;
using InSpectra.Lib.Modes.Help.Crawling;
using InSpectra.Lib.Modes.Hook.Execution;
using InSpectra.Lib.Modes.Native.Execution;
using InSpectra.Lib.Modes.Static.Inspection;
using InSpectra.Lib.Tooling.Process;
using InSpectra.Lib.Tooling.Results;

namespace InSpectra.Lib.Orchestration;

/// <summary>
/// Runs the installed-tool analyzer that matches a requested <see cref="AnalysisMode"/>,
/// writes artifacts into a temporary workspace, and returns a Contracts-level outcome.
/// Implements the public <see cref="IAcquisitionAnalysisDispatcher"/> so the app shell
/// can depend on Contracts only.
/// </summary>
internal sealed class AcquisitionAnalysisDispatcher
    : IAcquisitionAnalysisDispatcher, IAcquisitionAnalysisDispatcherInternal
{
    private readonly InstalledToolAnalyzer _helpAnalyzer;
    private readonly CliFxInstalledToolAnalysisSupport _cliFxAnalyzer;
    private readonly StaticInstalledToolAnalysisSupport _staticAnalyzer;
    private readonly HookInstalledToolAnalysisSupport _hookAnalyzer;
    private readonly NativeInstalledToolAnalysisSupport _nativeAnalyzer;

    public AcquisitionAnalysisDispatcher(
        InstalledToolAnalyzer helpAnalyzer,
        CliFxInstalledToolAnalysisSupport cliFxAnalyzer,
        StaticInstalledToolAnalysisSupport staticAnalyzer,
        HookInstalledToolAnalysisSupport hookAnalyzer,
        NativeInstalledToolAnalysisSupport nativeAnalyzer)
    {
        _helpAnalyzer = helpAnalyzer;
        _cliFxAnalyzer = cliFxAnalyzer;
        _staticAnalyzer = staticAnalyzer;
        _hookAnalyzer = hookAnalyzer;
        _nativeAnalyzer = nativeAnalyzer;
    }

    public async Task<AcquisitionAnalysisOutcome> TryAnalyzeAsync(
        CliTargetDescriptor target,
        string mode,
        string? framework,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => await TryAnalyzeCoreAsync(
            target,
            cleanupRoot: null,
            mode,
            framework,
            timeoutSeconds,
            persistCrawlCaptures: true,
            cancellationToken);

    public async Task<AcquisitionAnalysisOutcome> TryAnalyzeAsync(
        CliTargetDescriptor target,
        string? cleanupRoot,
        string mode,
        string? framework,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => await TryAnalyzeCoreAsync(
            target,
            cleanupRoot,
            mode,
            framework,
            timeoutSeconds,
            persistCrawlCaptures: false,
            cancellationToken);

    private async Task<AcquisitionAnalysisOutcome> TryAnalyzeCoreAsync(
        CliTargetDescriptor target,
        string? cleanupRoot,
        string mode,
        string? framework,
        int timeoutSeconds,
        bool persistCrawlCaptures,
        CancellationToken cancellationToken)
    {
        using var workspace = new TemporaryAnalysisWorkspace($"inspectra-{mode}");
        var outputDirectory = Path.Combine(workspace.RootPath, "artifacts");
        Directory.CreateDirectory(outputDirectory);

        var effectiveFramework = string.IsNullOrWhiteSpace(framework)
            ? target.CliFramework
            : framework;
        var result = NonSpectreResultSupport.CreateInitialResult(
            target.DisplayName,
            target.Version,
            target.CommandName,
            batchId: InspectraProductInfo.CliCommandName,
            attempt: 1,
            source: target.DisplayName,
            cliFramework: effectiveFramework,
            analysisMode: mode,
            analyzedAt: DateTimeOffset.UtcNow);
        result["nugetTitle"] = target.PackageTitle;
        result["nugetDescription"] = target.PackageDescription;

        var installedTool = CreateInstalledToolContext(target, cleanupRoot);
        await RunAnalyzerAsync(
            mode,
            result,
            target,
            installedTool,
            effectiveFramework,
            outputDirectory,
            timeoutSeconds,
            persistCrawlCaptures,
            cancellationToken);

        var disposition = result["disposition"]?.GetValue<string>();
        if (!string.Equals(disposition, "success", StringComparison.Ordinal))
        {
            return new AcquisitionAnalysisOutcome(
                false,
                mode,
                effectiveFramework,
                null,
                null,
                result["classification"]?.GetValue<string>(),
                result["failureMessage"]?.GetValue<string>());
        }

        var openCliPath = Path.Combine(outputDirectory, "opencli.json");
        var crawlPath = Path.Combine(outputDirectory, "crawl.json");
        return new AcquisitionAnalysisOutcome(
            true,
            mode,
            effectiveFramework,
            await File.ReadAllTextAsync(openCliPath, cancellationToken),
            File.Exists(crawlPath) ? await File.ReadAllTextAsync(crawlPath, cancellationToken) : null,
            null,
            null);
    }

    private async Task RunAnalyzerAsync(
        string mode,
        JsonObject result,
        CliTargetDescriptor target,
        InstalledToolContext installedTool,
        string? framework,
        string outputDirectory,
        int timeoutSeconds,
        bool persistCrawlCaptures,
        CancellationToken cancellationToken)
    {
        var request = new InstalledToolAnalysisRequest(
            result,
            target.Version,
            target.CommandName,
            outputDirectory,
            installedTool,
            target.WorkingDirectory,
            timeoutSeconds,
            persistCrawlCaptures);
        switch (mode)
        {
            case AnalysisMode.Help:
                await _helpAnalyzer.AnalyzeInstalledAsync(request, cancellationToken);
                return;
            case AnalysisMode.CliFx:
                await _cliFxAnalyzer.AnalyzeInstalledAsync(request, cancellationToken);
                return;
            case AnalysisMode.Static:
                await _staticAnalyzer.AnalyzeInstalledAsync(request, ResolveFrameworkOrThrow(mode, framework), cancellationToken);
                return;
            case AnalysisMode.Hook:
                await _hookAnalyzer.AnalyzeInstalledAsync(request, cancellationToken);
                return;
            case AnalysisMode.Native:
                await _nativeAnalyzer.AnalyzeInstalledAsync(request, cancellationToken);
                return;
            default:
                throw new InvalidOperationException($"Mode `{mode}` is not supported by the acquisition analysis dispatcher.");
        }
    }

    internal static InstalledToolContext CreateInstalledToolContext(
        CliTargetDescriptor target,
        string? cleanupRoot)
        => new(
            target.Environment,
            target.InstallDirectory,
            target.CommandPath,
            target.PreferredEntryPointPath,
            cleanupRoot);

    private static string ResolveFrameworkOrThrow(string mode, string? framework)
    {
        if (!string.IsNullOrWhiteSpace(framework))
        {
            return framework;
        }

        throw new CliUsageException($"{mode} mode requires a detectable or explicit CLI framework.");
    }
}

/// <summary>
/// Minimal temporary-directory helper used by <see cref="AcquisitionAnalysisDispatcher"/>.
/// Mirrors the semantics of <c>InSpectra.Lib.Execution.TemporaryWorkspace</c> but lives
/// inside the engine module so the dispatcher has no cross-project dependency.
/// </summary>
internal sealed class TemporaryAnalysisWorkspace : IDisposable
{
    public TemporaryAnalysisWorkspace(string prefix)
    {
        RootPath = Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup; analyzer failures should not propagate here.
        }
    }
}
