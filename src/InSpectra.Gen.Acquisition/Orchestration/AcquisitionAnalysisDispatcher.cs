using System.Text.Json.Nodes;
using InSpectra.Gen.Acquisition.Contracts;
using InSpectra.Gen.Core;
using InSpectra.Gen.Acquisition.Contracts.Providers;
using InSpectra.Gen.Acquisition.Modes.CliFx.Execution;
using InSpectra.Gen.Acquisition.Modes.Help.Crawling;
using InSpectra.Gen.Acquisition.Modes.Hook.Execution;
using InSpectra.Gen.Acquisition.Modes.Static.Inspection;
using InSpectra.Gen.Acquisition.Tooling.Process;
using InSpectra.Gen.Acquisition.Tooling.Results;

namespace InSpectra.Gen.Acquisition.Orchestration;

/// <summary>
/// Runs the installed-tool analyzer that matches a requested <see cref="AnalysisMode"/>,
/// writes artifacts into a temporary workspace, and returns a Contracts-level outcome.
/// Implements the public <see cref="IAcquisitionAnalysisDispatcher"/> so the app shell
/// can depend on Contracts only.
/// </summary>
internal sealed class AcquisitionAnalysisDispatcher : IAcquisitionAnalysisDispatcher
{
    private readonly InstalledToolAnalyzer _helpAnalyzer;
    private readonly CliFxInstalledToolAnalysisSupport _cliFxAnalyzer;
    private readonly StaticInstalledToolAnalysisSupport _staticAnalyzer;
    private readonly HookInstalledToolAnalysisSupport _hookAnalyzer;

    public AcquisitionAnalysisDispatcher(
        InstalledToolAnalyzer helpAnalyzer,
        CliFxInstalledToolAnalysisSupport cliFxAnalyzer,
        StaticInstalledToolAnalysisSupport staticAnalyzer,
        HookInstalledToolAnalysisSupport hookAnalyzer)
    {
        _helpAnalyzer = helpAnalyzer;
        _cliFxAnalyzer = cliFxAnalyzer;
        _staticAnalyzer = staticAnalyzer;
        _hookAnalyzer = hookAnalyzer;
    }

    public async Task<AcquisitionAnalysisOutcome> TryAnalyzeAsync(
        CliTargetDescriptor target,
        string mode,
        string? framework,
        int timeoutSeconds,
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

        var installedTool = new InstalledToolContext(
            target.Environment,
            target.InstallDirectory,
            target.CommandPath,
            target.PreferredEntryPointPath);
        await RunAnalyzerAsync(
            mode,
            result,
            target,
            installedTool,
            effectiveFramework,
            outputDirectory,
            timeoutSeconds,
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
        CancellationToken cancellationToken)
    {
        var request = new InstalledToolAnalysisRequest(
            result,
            target.Version,
            target.CommandName,
            outputDirectory,
            installedTool,
            target.WorkingDirectory,
            timeoutSeconds);
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
            default:
                throw new InvalidOperationException($"Mode `{mode}` is not supported by the discovery analyzer bridge.");
        }
    }

    private static string ResolveFrameworkOrThrow(string mode, string? framework)
    {
        if (!string.IsNullOrWhiteSpace(framework))
        {
            return framework;
        }

        throw new CliUsageException($"`{mode}` mode requires a detectable or explicit `--cli-framework`.");
    }
}

/// <summary>
/// Minimal temporary-directory helper used by <see cref="AcquisitionAnalysisDispatcher"/>.
/// Mirrors the semantics of <c>InSpectra.Gen.Execution.TemporaryWorkspace</c> but lives
/// inside the Acquisition module so the dispatcher has no cross-project dependency.
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
