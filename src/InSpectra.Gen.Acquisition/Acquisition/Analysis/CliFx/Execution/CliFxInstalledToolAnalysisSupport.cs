namespace InSpectra.Gen.Acquisition.Analysis.CliFx.Execution;

using InSpectra.Gen.Acquisition.Infrastructure.Paths;

using InSpectra.Gen.Acquisition.OpenCli.Documents;

using InSpectra.Gen.Acquisition.Analysis.NonSpectre;

using InSpectra.Gen.Acquisition.Analysis.CliFx.Artifacts;

using InSpectra.Gen.Acquisition.Infrastructure.Artifacts;

using InSpectra.Gen.Acquisition.Analysis.CliFx.Crawling;

using InSpectra.Gen.Acquisition.Infrastructure.Commands;

using InSpectra.Gen.Acquisition.Analysis.CliFx.OpenCli;

using InSpectra.Gen.Acquisition.Analysis.CliFx.Metadata;

using InSpectra.Gen.Acquisition.Analysis;
using InSpectra.Gen.Acquisition.Analysis.OpenCli;
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
            result,
            version,
            commandName,
            outputDirectory,
            installedTool,
            tempRoot,
            commandTimeoutSeconds,
            cancellationToken);
    }

    internal async Task AnalyzeInstalledAsync(
        JsonObject result,
        string version,
        string commandName,
        string outputDirectory,
        InstalledToolContext installedTool,
        string workingDirectory,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        var crawlStopwatch = Stopwatch.StartNew();
        var staticCommands = NormalizeCommandLookup(_metadataInspector.Inspect(installedTool.InstallDirectory));
        var crawler = new CliFxHelpCrawler(_runtime);
        var crawl = await crawler.CrawlAsync(installedTool.CommandPath, workingDirectory, installedTool.Environment, commandTimeoutSeconds, cancellationToken);
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

        result["timings"]!.AsObject()["crawlMs"] = (int)Math.Round(crawlStopwatch.Elapsed.TotalMilliseconds);
        result["coverage"] = coverageJson;
        if (outputLimitExceededCommands.Length > 0)
        {
            NonSpectreResultSupport.ApplyTerminalFailure(
                result,
                phase: "crawl",
                classification: "clifx-output-too-large",
                $"{ProcessOutputCaptureSupport.BuildOutputLimitExceededMessage()} Affected commands: {string.Join(", ", outputLimitExceededCommands)}.");
            return;
        }

        if (guardrailFailureMessages.Length > 0)
        {
            NonSpectreResultSupport.ApplyTerminalFailure(
                result,
                phase: "crawl",
                classification: "clifx-crawl-budget-exceeded",
                string.Join(" ", guardrailFailureMessages));
            return;
        }

        if (!CommandInstallationSupport.TryWriteCrawlArtifactOrApplyFailure(
            outputDirectory,
            result,
            CrawlArtifactBuilder.Build(
                crawl.Documents.Count,
                crawl.Captures,
                CliFxCrawlArtifactSupport.BuildMetadata(staticCommands, coverageJson))))
        {
            return;
        }

        if (crawl.Documents.Count == 0 && staticCommands.Count == 0)
        {
            if (string.Equals(coverage.RuntimeCompatibilityMode, "missing-framework", StringComparison.Ordinal))
            {
                NonSpectreResultSupport.ApplyTerminalFailure(
                    result,
                    phase: "crawl",
                    classification: "clifx-runtime-blocked",
                    DotnetRuntimeCompatibilitySupport.BuildMissingFrameworkFailureMessage(
                        coverage.RuntimeBlockedCommands,
                        coverage.RequiredFrameworks));
                return;
            }

            NonSpectreResultSupport.ApplyTerminalFailure(
                result,
                phase: "crawl",
                classification: "clifx-crawl-empty",
                "No CliFx help documents or metadata commands could be captured from the installed tool.");
            return;
        }

        var openCliDocument = _openCliBuilder.Build(commandName, version, staticCommands, crawl.Documents);
        if (!string.IsNullOrWhiteSpace(result["cliFramework"]?.GetValue<string>()))
        {
            openCliDocument["x-inspectra"]!.AsObject()["cliFramework"] = result["cliFramework"]!.GetValue<string>();
        }

        OpenCliDocumentSanitizer.ApplyNuGetMetadata(
            openCliDocument,
            result["nugetTitle"]?.GetValue<string>(),
            result["nugetDescription"]?.GetValue<string>());

        OpenCliAnalysisArtifactValidationSupport.TryWriteValidatedArtifact(
            result,
            outputDirectory,
            openCliDocument,
            successClassification: "clifx-crawl",
            artifactSource: "crawled-from-clifx-help");
    }

    private static Dictionary<string, CliFxCommandDefinition> NormalizeCommandLookup(IReadOnlyDictionary<string, CliFxCommandDefinition> commands)
        => new(commands, StringComparer.OrdinalIgnoreCase);
}
