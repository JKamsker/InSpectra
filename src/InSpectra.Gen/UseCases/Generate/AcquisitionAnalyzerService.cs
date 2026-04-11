using System.Text.Json.Nodes;
using InSpectra.Gen.Acquisition.Modes.CliFx.Execution;
using InSpectra.Gen.Acquisition.Modes.Hook;
using InSpectra.Gen.Acquisition.Contracts.Results;
using InSpectra.Gen.Acquisition.Modes.Help.Crawling;
using InSpectra.Gen.Acquisition.Tooling.Process;
using InSpectra.Gen.Acquisition.Contracts;
using InSpectra.Gen.Acquisition.Modes.Static.Inspection;
using InSpectra.Gen.Acquisition.Modes.Static.Projection;
using InSpectra.Gen.Acquisition.Contracts.Exceptions;
using InSpectra.Gen.UseCases.Generate.Requests;

namespace InSpectra.Gen.UseCases.Generate;

internal sealed record AcquisitionAnalysisOutcome(
    bool Success,
    string Mode,
    string? Framework,
    string? OpenCliJson,
    string? CrawlJson,
    string? FailureClassification,
    string? FailureMessage);

internal sealed class AcquisitionAnalyzerService
{
    private readonly InstalledToolAnalyzer _helpAnalyzer;
    private readonly CliFxInstalledToolAnalysisSupport _cliFxAnalyzer;
    private readonly StaticInstalledToolAnalysisSupport _staticAnalyzer;
    private readonly HookInstalledToolAnalysisSupport _hookAnalyzer;

    public AcquisitionAnalyzerService(
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

    internal async Task<AcquisitionAnalysisOutcome> TryAnalyzeAsync(
        MaterializedCliTarget target,
        OpenCliMode mode,
        string? framework,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        using var workspace = new TemporaryWorkspace($"inspectra-{OpenCliModePlanner.ToModeValue(mode)}");
        var outputDirectory = Path.Combine(workspace.RootPath, "artifacts");
        Directory.CreateDirectory(outputDirectory);

        var analysisMode = OpenCliModePlanner.ToModeValue(mode);
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
            analysisMode: analysisMode,
            analyzedAt: DateTimeOffset.UtcNow);
        result["nugetTitle"] = target.PackageTitle;
        result["nugetDescription"] = target.PackageDescription;

        var installedTool = new InstalledToolContext(target.Environment, target.InstallDirectory, target.CommandPath, target.PreferredEntryPointPath);
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
                analysisMode,
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
            analysisMode,
            effectiveFramework,
            await File.ReadAllTextAsync(openCliPath, cancellationToken),
            File.Exists(crawlPath) ? await File.ReadAllTextAsync(crawlPath, cancellationToken) : null,
            null,
            null);
    }

    private async Task RunAnalyzerAsync(
        OpenCliMode mode,
        JsonObject result,
        MaterializedCliTarget target,
        InstalledToolContext installedTool,
        string? framework,
        string outputDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var request = new InstalledToolAnalysisRequest(result, target.Version, target.CommandName, outputDirectory, installedTool, target.WorkingDirectory, timeoutSeconds);
        switch (mode)
        {
            case OpenCliMode.Help:
                await _helpAnalyzer.AnalyzeInstalledAsync(request, cancellationToken);
                return;
            case OpenCliMode.CliFx:
                await _cliFxAnalyzer.AnalyzeInstalledAsync(request, cancellationToken);
                return;
            case OpenCliMode.Static:
                await _staticAnalyzer.AnalyzeInstalledAsync(request, ResolveFrameworkOrThrow(mode, framework), cancellationToken);
                return;
            case OpenCliMode.Hook:
                await _hookAnalyzer.AnalyzeInstalledAsync(request, cancellationToken);
                return;
            default:
                throw new InvalidOperationException($"Mode `{mode}` is not supported by the discovery analyzer bridge.");
        }
    }

    private static string ResolveFrameworkOrThrow(OpenCliMode mode, string? framework)
    {
        if (!string.IsNullOrWhiteSpace(framework))
        {
            return framework;
        }

        throw new CliUsageException($"`{OpenCliModePlanner.ToModeValue(mode)}` mode requires a detectable or explicit `--cli-framework`.");
    }
}
