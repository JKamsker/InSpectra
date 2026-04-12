namespace InSpectra.Gen.Engine.Modes.CliFx.Execution;

using InSpectra.Gen.Engine.Tooling.Paths;

using InSpectra.Gen.Engine.Tooling.DocumentPipeline.Documents;

using InSpectra.Gen.Engine.Tooling.Results;

using InSpectra.Gen.Engine.Modes.CliFx.Crawling;

using InSpectra.Gen.Engine.Tooling.Process;

using InSpectra.Gen.Engine.Modes.CliFx.Projection;

using InSpectra.Gen.Engine.Modes.CliFx.Metadata;

using InSpectra.Gen.Engine.Contracts;
using System.Diagnostics;
using System.Text.Json.Nodes;

internal sealed class CliFxInstalledToolAnalysisSupport
{
    private readonly CommandRuntime _runtime;
    private readonly CliFxMetadataInspector _metadataInspector;
    private readonly CliFxOpenCliBuilder _openCliBuilder;
    private readonly CliFxCoverageClassifier _coverageClassifier;

    public CliFxInstalledToolAnalysisSupport(
        CommandRuntime runtime,
        CliFxMetadataInspector metadataInspector,
        CliFxOpenCliBuilder openCliBuilder,
        CliFxCoverageClassifier coverageClassifier)
    {
        _runtime = runtime;
        _metadataInspector = metadataInspector;
        _openCliBuilder = openCliBuilder;
        _coverageClassifier = coverageClassifier;
    }

    public async Task AnalyzeAsync(
        JsonObject result,
        string packageId,
        string version,
        string commandName,
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
            cancellationToken);
    }

    internal async Task AnalyzeInstalledAsync(
        InstalledToolAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var crawlStopwatch = Stopwatch.StartNew();
        var staticCommands = NormalizeCommandLookup(_metadataInspector.Inspect(request.InstalledTool.InstallDirectory));
        var crawler = new CliFxHelpCrawler(_runtime);
        var crawl = await crawler.CrawlAsync(
            request.InstalledTool.CommandPath,
            request.WorkingDirectory,
            request.InstalledTool.Environment,
            request.CommandTimeoutSeconds,
            request.InstalledTool.CleanupRoot,
            cancellationToken);
        crawlStopwatch.Stop();
        var coverage = _coverageClassifier.Classify(staticCommands.Count, crawl);
        var coverageJson = coverage.ToJsonObject();
        var outputLimitExceededCommands = crawl.CaptureSummaries.Values
            .Where(summary => summary.OutputLimitExceeded)
            .Select(summary => string.IsNullOrWhiteSpace(summary.Command) ? "<root>" : summary.Command)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var guardrailFailureMessages = crawl.CaptureSummaries.Values
            .Select(summary => summary.GuardrailFailureMessage)
            .Concat([crawl.GuardrailFailureMessage])
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();

        request.Result["timings"]!.AsObject()["crawlMs"] = (int)Math.Round(crawlStopwatch.Elapsed.TotalMilliseconds);
        request.Result["coverage"] = coverageJson;
        if (outputLimitExceededCommands.Length > 0)
        {
            NonSpectreResultSupport.ApplyTerminalFailure(
                request.Result,
                phase: "crawl",
                classification: "clifx-output-too-large",
                $"{ProcessOutputCaptureSupport.BuildOutputLimitExceededMessage()} Affected commands: {string.Join(", ", outputLimitExceededCommands)}.");
            return;
        }

        if (guardrailFailureMessages.Length > 0)
        {
            NonSpectreResultSupport.ApplyTerminalFailure(
                request.Result,
                phase: "crawl",
                classification: "clifx-crawl-budget-exceeded",
                string.Join(" ", guardrailFailureMessages));
            return;
        }

        if (crawl.Documents.Count == 0 && staticCommands.Count == 0)
        {
            if (string.Equals(coverage.RuntimeCompatibilityMode, "missing-framework", StringComparison.Ordinal))
            {
                NonSpectreResultSupport.ApplyTerminalFailure(
                    request.Result,
                    phase: "crawl",
                    classification: "clifx-runtime-blocked",
                    DotnetRuntimeCompatibilitySupport.BuildMissingFrameworkFailureMessage(
                        coverage.RuntimeBlockedCommands,
                        coverage.RequiredFrameworks));
                return;
            }

            NonSpectreResultSupport.ApplyTerminalFailure(
                request.Result,
                phase: "crawl",
                classification: "clifx-crawl-empty",
                "No CliFx help documents or metadata commands could be captured from the installed tool.");
            return;
        }

        var openCliDocument = _openCliBuilder.Build(request.CommandName, request.Version, staticCommands, crawl.Documents);
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
            successClassification: "clifx-crawl",
            artifactSource: "crawled-from-clifx-help");
    }

    private static Dictionary<string, CliFxCommandDefinition> NormalizeCommandLookup(IReadOnlyDictionary<string, CliFxCommandDefinition> commands)
        => new(commands, StringComparer.OrdinalIgnoreCase);
}
