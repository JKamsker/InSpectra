namespace InSpectra.Gen.Engine.Modes.Help.Crawling;

using InSpectra.Gen.Engine.Tooling.Paths;

using InSpectra.Gen.Engine.Tooling.DocumentPipeline.Documents;

using InSpectra.Gen.Engine.Tooling.Results;

using InSpectra.Gen.Engine.Modes.Help.Projection;
using InSpectra.Gen.Engine.Contracts.Documents;

using InSpectra.Gen.Engine.Tooling.Process;

using InSpectra.Gen.Engine.Contracts;
using System.Diagnostics;
using System.Text.Json.Nodes;

internal sealed class InstalledToolAnalyzer
{
    private readonly CommandRuntime _runtime;
    private readonly OpenCliBuilder _openCliBuilder;

    public InstalledToolAnalyzer(CommandRuntime runtime, OpenCliBuilder openCliBuilder)
    {
        _runtime = runtime;
        _openCliBuilder = openCliBuilder;
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
        var crawler = new Crawler(_runtime);
        var crawl = await crawler.CrawlAsync(
            request.InstalledTool.CommandPath,
            request.CommandName,
            request.WorkingDirectory,
            request.InstalledTool.Environment,
            request.CommandTimeoutSeconds,
            request.InstalledTool.CleanupRoot,
            cancellationToken);
        crawlStopwatch.Stop();
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
        if (outputLimitExceededCommands.Length > 0)
        {
            NonSpectreResultSupport.ApplyTerminalFailure(
                request.Result,
                phase: "crawl",
                classification: "help-crawl-output-too-large",
                $"{ProcessOutputCaptureSupport.BuildOutputLimitExceededMessage()} Affected commands: {string.Join(", ", outputLimitExceededCommands)}.");
            return;
        }

        if (guardrailFailureMessages.Length > 0)
        {
            NonSpectreResultSupport.ApplyTerminalFailure(
                request.Result,
                phase: "crawl",
                classification: "help-crawl-budget-exceeded",
                string.Join(" ", guardrailFailureMessages));
            return;
        }

        if (crawl.Documents.Count == 0)
        {
            var runtimeIssues = crawl.CaptureSummaries.Values
                .Select(summary => DotnetRuntimeCompatibilitySupport.DetectMissingFramework(
                    summary.Command,
                    summary.Stdout,
                    summary.Stderr))
                .Where(issue => issue is not null)
                .Cast<DotnetRuntimeIssue>()
                .ToArray();
            if (runtimeIssues.Length > 0)
            {
                NonSpectreResultSupport.ApplyTerminalFailure(
                    request.Result,
                    phase: "crawl",
                    classification: "help-crawl-runtime-blocked",
                    DotnetRuntimeCompatibilitySupport.BuildMissingFrameworkFailureMessage(
                        runtimeIssues.Select(issue => issue.Command).ToArray(),
                        runtimeIssues
                            .Where(issue => issue.Requirement is not null)
                            .Select(issue => issue.Requirement!)
                            .Distinct()
                            .ToArray()));
                return;
            }

            var platformBlockedMessage = crawl.CaptureSummaries.Values
                .SelectMany(summary => new[] { summary.Stdout, summary.Stderr })
                .FirstOrDefault(DocumentInspector.LooksLikePlatformBlockedPayload);
            if (!string.IsNullOrWhiteSpace(platformBlockedMessage))
            {
                NonSpectreResultSupport.ApplyTerminalFailure(
                    request.Result,
                    phase: "crawl",
                    classification: "help-crawl-platform-blocked",
                    platformBlockedMessage);
                return;
            }

            NonSpectreResultSupport.ApplyTerminalFailure(
                request.Result,
                phase: "crawl",
                classification: "help-crawl-empty",
                "No help documents could be captured from the installed tool.");
            return;
        }

        var openCliDocument = _openCliBuilder.Build(request.CommandName, request.Version, crawl.Documents);
        if (!string.IsNullOrWhiteSpace(request.Result["cliFramework"]?.GetValue<string>()))
        {
            openCliDocument["x-inspectra"]!["cliFramework"] = request.Result["cliFramework"]!.GetValue<string>();
        }

        OpenCliDocumentSanitizer.ApplyNuGetMetadata(
            openCliDocument,
            request.Result["nugetTitle"]?.GetValue<string>(),
            request.Result["nugetDescription"]?.GetValue<string>());

        OpenCliAnalysisArtifactValidationSupport.TryWriteValidatedArtifact(
            request.Result,
            request.OutputDirectory,
            openCliDocument,
            successClassification: "help-crawl",
            artifactSource: "crawled-from-help");
    }
}
