namespace InSpectra.Lib.Tests.Help;

using InSpectra.Lib.Contracts;
using InSpectra.Lib.Modes.Help.Crawling;
using InSpectra.Lib.Modes.Help.Projection;
using InSpectra.Lib.Tests.TestSupport;
using InSpectra.Lib.Tooling.Process;
using InSpectra.Lib.Tooling.Results;

using System.Text.Json.Nodes;

public sealed class InstalledToolAnalyzerFallbackTests
{
    [Fact]
    public async Task AnalyzeInstalledAsync_Publishes_MetadataOnly_When_Help_Is_Not_Parseable()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var result = CreateResult();
        var analyzer = new InstalledToolAnalyzer(new EmptyHelpCommandRuntime(), new OpenCliBuilder());

        await analyzer.AnalyzeInstalledAsync(CreateRequest(result, tempDirectory.Path), CancellationToken.None);

        Assert.Equal("success", result[ResultKey.Disposition]?.GetValue<string>());
        Assert.Equal("metadata-only", result[ResultKey.Classification]?.GetValue<string>());
        var document = LoadOpenCli(tempDirectory.Path);
        Assert.Equal("metadata-only", document["x-inspectra"]?["artifactSource"]?.GetValue<string>());
        Assert.Equal("Demo package", document["info"]?["title"]?.GetValue<string>());
    }

    [Fact]
    public async Task AnalyzeInstalledAsync_Publishes_Partial_When_Crawl_Guardrail_Truncates_Useful_Help()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var result = CreateResult();
        var analyzer = new InstalledToolAnalyzer(new FanoutHelpCommandRuntime(), new OpenCliBuilder());

        await analyzer.AnalyzeInstalledAsync(CreateRequest(result, tempDirectory.Path), CancellationToken.None);

        Assert.Equal("success", result[ResultKey.Disposition]?.GetValue<string>());
        Assert.Equal("help-crawl-partial", result[ResultKey.Classification]?.GetValue<string>());
        Assert.True(File.Exists(Path.Combine(tempDirectory.Path, "crawl.json")));

        var document = LoadOpenCli(tempDirectory.Path);
        Assert.True(document["x-inspectra"]?["crawlTruncated"]?.GetValue<bool>());
        Assert.Contains("child budget", document["x-inspectra"]?["truncationReason"]?.GetValue<string>());
    }

    private static InstalledToolAnalysisRequest CreateRequest(JsonObject result, string outputDirectory)
        => new(
            result,
            Version: "1.2.3",
            CommandName: "demo",
            OutputDirectory: outputDirectory,
            InstalledTool: new InstalledToolContext(
                Environment: new Dictionary<string, string>(),
                InstallDirectory: outputDirectory,
                CommandPath: "demo"),
            WorkingDirectory: outputDirectory,
            CommandTimeoutSeconds: 30,
            PersistCrawlCaptures: true);

    private static JsonObject CreateResult()
    {
        var result = NonSpectreResultSupport.CreateInitialResult(
            "Demo.Tool",
            "1.2.3",
            "demo",
            batchId: "test",
            attempt: 1,
            source: "test",
            cliFramework: null,
            analysisMode: AnalysisMode.Help,
            analyzedAt: DateTimeOffset.UtcNow);
        result["nugetTitle"] = "Demo package";
        result["nugetDescription"] = "Package metadata description.";
        return result;
    }

    private static JsonObject LoadOpenCli(string outputDirectory)
        => JsonNode.Parse(File.ReadAllText(Path.Combine(outputDirectory, "opencli.json")))!.AsObject();

    private sealed class EmptyHelpCommandRuntime : CommandRuntime
    {
        public override Task<ProcessResult> InvokeProcessCaptureAsync(
            string filePath,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environment,
            int timeoutSeconds,
            string? sandboxRoot,
            CancellationToken cancellationToken)
            => Task.FromResult(new ProcessResult(
                Status: "failed",
                TimedOut: false,
                ExitCode: 1,
                DurationMs: 1,
                Stdout: string.Empty,
                Stderr: string.Empty));
    }

    private sealed class FanoutHelpCommandRuntime : CommandRuntime
    {
        public override Task<ProcessResult> InvokeProcessCaptureAsync(
            string filePath,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environment,
            int timeoutSeconds,
            string? sandboxRoot,
            CancellationToken cancellationToken)
        {
            var commandRows = Enumerable.Range(1, 97)
                .Select(index => $"  cmd{index:D2}         Command {index}.")
                .ToArray();
            var help =
                """
                demo

                USAGE
                  demo [command] [options]

                OPTIONS
                  --verbose  Verbose output.

                COMMANDS
                """ + Environment.NewLine + string.Join(Environment.NewLine, commandRows);

            return Task.FromResult(new ProcessResult(
                Status: "ok",
                TimedOut: false,
                ExitCode: 0,
                DurationMs: 1,
                Stdout: help,
                Stderr: string.Empty));
        }
    }
}
