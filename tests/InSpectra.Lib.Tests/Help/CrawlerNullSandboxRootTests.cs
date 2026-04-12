namespace InSpectra.Lib.Tests.Help;

using InSpectra.Lib.Modes.Help.Crawling;
using InSpectra.Lib.Tooling.Process;
using InSpectra.Lib.Tests.TestSupport;

public sealed class CrawlerNullSandboxRootTests
{
    [Fact]
    public async Task CrawlAsync_Leaves_Sandbox_Root_Null_When_Not_Provided()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var workingDirectory = Path.Combine(tempDirectory.Path, "workspace");
        Directory.CreateDirectory(workingDirectory);

        var runtime = new RecordingCommandRuntime();
        var crawler = new Crawler(runtime);

        var result = await crawler.CrawlAsync(
            commandPath: "demo",
            rootCommandName: "demo",
            workingDirectory: workingDirectory,
            environment: new Dictionary<string, string>(),
            timeoutSeconds: 30,
            sandboxCleanupRoot: null,
            cancellationToken: CancellationToken.None);

        Assert.True(result.Documents.ContainsKey(string.Empty));
        Assert.NotEmpty(runtime.Invocations);
        Assert.All(runtime.Invocations, invocation =>
        {
            Assert.Equal(workingDirectory, invocation.WorkingDirectory);
            Assert.Null(invocation.SandboxRoot);
        });
    }

    private sealed class RecordingCommandRuntime : CommandRuntime
    {
        public List<(string WorkingDirectory, string? SandboxRoot)> Invocations { get; } = [];

        public override Task<ProcessResult> InvokeProcessCaptureAsync(
            string filePath,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environment,
            int timeoutSeconds,
            string? sandboxRoot,
            CancellationToken cancellationToken)
        {
            Invocations.Add((workingDirectory, sandboxRoot));
            return Task.FromResult(new ProcessResult(
                Status: "ok",
                TimedOut: false,
                ExitCode: 0,
                DurationMs: 1,
                Stdout:
                """
                demo

                USAGE
                  demo [options]
                """,
                Stderr: string.Empty));
        }
    }
}
