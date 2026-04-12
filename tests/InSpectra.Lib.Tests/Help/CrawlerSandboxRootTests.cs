namespace InSpectra.Lib.Tests.Help;

using InSpectra.Lib.Modes.Help.Crawling;
using InSpectra.Lib.Tooling.Process;
using InSpectra.Lib.Tests.TestSupport;

public sealed class CrawlerSandboxRootTests
{
    [Fact]
    public async Task CrawlAsync_Uses_Engine_Sandbox_Root_For_Cleanup_While_Keeping_Working_Directory()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var sandboxRoot = Path.Combine(tempDirectory.Path, "sandbox");
        var workingDirectory = Path.Combine(tempDirectory.Path, "workspace");
        Directory.CreateDirectory(sandboxRoot);
        Directory.CreateDirectory(workingDirectory);

        var runtime = new RecordingCommandRuntime();
        var crawler = new Crawler(runtime);
        var sandboxEnvironment = runtime.CreateSandboxEnvironment(sandboxRoot);

        var result = await crawler.CrawlAsync(
            commandPath: "demo",
            rootCommandName: "demo",
            workingDirectory: workingDirectory,
            environment: sandboxEnvironment.Values,
            timeoutSeconds: 30,
            sandboxCleanupRoot: Path.GetFullPath(sandboxRoot),
            cancellationToken: CancellationToken.None);

        Assert.True(result.Documents.ContainsKey(string.Empty));
        Assert.NotEmpty(runtime.Invocations);
        Assert.All(runtime.Invocations, invocation =>
        {
            Assert.Equal(workingDirectory, invocation.WorkingDirectory);
            Assert.Equal(Path.GetFullPath(sandboxRoot), invocation.SandboxRoot);
            Assert.NotEqual(invocation.WorkingDirectory, invocation.SandboxRoot);
        });
        Assert.Contains(result.Documents[string.Empty].Options, option => option.Key == "--verbose");
    }

    private static CommandRuntime.ProcessResult HelpResult()
        => new(
            Status: "ok",
            TimedOut: false,
            ExitCode: 0,
            DurationMs: 1,
            Stdout:
            """
            demo

            USAGE
              demo [options]

            OPTIONS
              --verbose  Verbose output.
            """,
            Stderr: string.Empty);

    private sealed class RecordingCommandRuntime : CommandRuntime
    {
        public List<InvocationRecord> Invocations { get; } = [];

        public override Task<ProcessResult> InvokeProcessCaptureAsync(
            string filePath,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environment,
            int timeoutSeconds,
            string? sandboxRoot,
            CancellationToken cancellationToken)
        {
            Invocations.Add(new InvocationRecord(
                argumentList.ToArray(),
                workingDirectory,
                sandboxRoot));
            return Task.FromResult(HelpResult());
        }
    }

    private sealed record InvocationRecord(
        string[] Arguments,
        string WorkingDirectory,
        string? SandboxRoot);
}
