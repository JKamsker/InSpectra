namespace InSpectra.Gen.Acquisition.Modes.Static.Inspection;

using InSpectra.Gen.Acquisition.Tooling.FrameworkDetection;
using InSpectra.Gen.Acquisition.Tooling.Paths;

using InSpectra.Gen.Acquisition.Tooling.DocumentPipeline.Documents;

using InSpectra.Gen.Acquisition.Contracts.Providers;
using InSpectra.Gen.Acquisition.Contracts.Results;

using InSpectra.Gen.Acquisition.Tooling.Process;

using InSpectra.Gen.Acquisition.Modes.Static.Projection;

using InSpectra.Gen.Acquisition.Contracts;
using System.Diagnostics;
using System.Text.Json.Nodes;

internal sealed class StaticInstalledToolAnalysisSupport
{
    private readonly StaticAnalysisRuntime _runtime;
    private readonly StaticAnalysisAssemblyInspectionSupport _assemblyInspectionSupport;
    private readonly StaticAnalysisOpenCliBuilder _openCliBuilder;
    private readonly StaticAnalysisCoverageClassifier _coverageClassifier;
    private readonly IHelpCrawler _helpCrawler;

    public StaticInstalledToolAnalysisSupport(
        StaticAnalysisRuntime runtime,
        StaticAnalysisAssemblyInspectionSupport assemblyInspectionSupport,
        StaticAnalysisOpenCliBuilder openCliBuilder,
        StaticAnalysisCoverageClassifier coverageClassifier,
        IHelpCrawler helpCrawler)
    {
        _runtime = runtime;
        _assemblyInspectionSupport = assemblyInspectionSupport;
        _openCliBuilder = openCliBuilder;
        _coverageClassifier = coverageClassifier;
        _helpCrawler = helpCrawler;
    }

    public async Task AnalyzeAsync(
        JsonObject result,
        string packageId,
        string version,
        string commandName,
        string cliFramework,
        string outputDirectory,
        string tempRoot,
        int installTimeoutSeconds,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        var installedTool = await CommandInstallationSupport.InstallToolAsync(
            _runtime,
            result,
            packageId,
            version,
            commandName,
            tempRoot,
            installTimeoutSeconds,
            cancellationToken);
        if (installedTool is null)
        {
            return;
        }

        await AnalyzeInstalledAsync(
            new InstalledToolAnalysisRequest(result, version, commandName, outputDirectory, installedTool, tempRoot, commandTimeoutSeconds),
            cliFramework,
            cancellationToken);
    }

    internal async Task AnalyzeInstalledAsync(
        InstalledToolAnalysisRequest request,
        string cliFramework,
        CancellationToken cancellationToken)
    {
        var crawlStopwatch = Stopwatch.StartNew();
        var preferredEntryPointPath = InstalledDotnetToolCommandSupport.TryResolve(
            request.InstalledTool.InstallDirectory,
            request.CommandName)?.EntryPointPath;
        var inspection = _assemblyInspectionSupport.InspectAssemblies(
            request.InstalledTool.InstallDirectory,
            cliFramework,
            preferredEntryPointPath);
        if (ApplyInspectionFailure(request.Result, inspection))
        {
            return;
        }

        var crawl = await _helpCrawler.CrawlAsync(request.InstalledTool.CommandPath, request.CommandName, request.WorkingDirectory, request.InstalledTool.Environment, request.CommandTimeoutSeconds, cancellationToken);
        crawlStopwatch.Stop();

        var staticCommands = inspection.Commands;
        var coverage = _coverageClassifier.Classify(staticCommands.Count, crawl.Documents.Count, crawl.Captures);
        var coverageJson = coverage.ToJsonObject();

        request.Result["timings"]!.AsObject()["crawlMs"] = (int)Math.Round(crawlStopwatch.Elapsed.TotalMilliseconds);
        request.Result["coverage"] = coverageJson;

        if (crawl.Documents.Count == 0 && staticCommands.Count == 0)
        {
            NonSpectreResultSupport.ApplyTerminalFailure(
                request.Result,
                phase: "crawl",
                classification: "static-crawl-empty",
                "No help documents or static metadata could be captured from the installed tool.");
            return;
        }

        var resolvedFramework = ResolveFrameworkName(cliFramework);
        var openCliDocument = _openCliBuilder.Build(request.CommandName, request.Version, resolvedFramework, staticCommands, crawl.Documents);
        if (!string.IsNullOrWhiteSpace(request.Result["cliFramework"]?.GetValue<string>()))
        {
            openCliDocument["x-inspectra"]!.AsObject()["cliFramework"] = request.Result["cliFramework"]!.GetValue<string>();
        }

        OpenCliDocumentSanitizer.ApplyNuGetMetadata(
            openCliDocument,
            request.Result["nugetTitle"]?.GetValue<string>(),
            request.Result["nugetDescription"]?.GetValue<string>());

        OpenCliAnalysisArtifactValidationSupport.TryWriteValidatedArtifact(
            request.Result,
            request.OutputDirectory,
            openCliDocument,
            successClassification: "static-crawl",
            artifactSource: "static-analysis");
    }

    private static bool ApplyInspectionFailure(JsonObject result, StaticAnalysisAssemblyInspectionResult inspection)
    {
        if (inspection.InspectionOutcome is "framework-not-found" or "no-reader")
        {
            result["inspectionOutcome"] = inspection.InspectionOutcome;
            result["cliFramework"] = null;
            NonSpectreResultSupport.ApplyTerminalFailure(
                result,
                phase: "static-analysis",
                classification: "custom-parser",
                $"Claimed framework '{inspection.ClaimedFramework}' was not found in any assembly. Tool likely uses a custom argument parser.");
            return true;
        }

        if (inspection.InspectionOutcome is "no-attributes")
        {
            result["inspectionOutcome"] = inspection.InspectionOutcome;
            NonSpectreResultSupport.ApplyTerminalFailure(
                result,
                phase: "static-analysis",
                classification: "custom-parser-no-attributes",
                $"Framework '{inspection.ClaimedFramework}' assembly found in {inspection.ScannedModuleCount} module(s) but no recognizable attributes detected. Tool may use fluent API or non-standard configuration.");
            return true;
        }

        return false;
    }

    private static string ResolveFrameworkName(string cliFramework)
        => CliFrameworkProviderRegistry.ResolveStaticAnalysisAdapter(cliFramework)?.FrameworkName ?? cliFramework;
}
