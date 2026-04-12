namespace InSpectra.Lib.Tests.Help;

using InSpectra.Lib.Contracts.Crawling;

using InSpectra.Lib.Modes.CliFx.Crawling;
using InSpectra.Lib.Modes.Help.Crawling;
using InSpectra.Lib.Tooling.Process;
using InSpectra.Lib.Tests.TestSupport;

using Xunit;

public sealed class CliFxHelpCrawlerTests
{
    [Fact]
    public async Task CrawlAsync_Retries_With_DotnetRollForward_When_Shared_Runtime_Is_Missing()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var runtime = new FakeCommandRuntime();
        var crawler = new CliFxHelpCrawler(runtime);
        var sandboxRoot = Path.Combine(tempDirectory.Path, "sandbox");
        var workingDirectory = Path.Combine(tempDirectory.Path, "workspace");
        Directory.CreateDirectory(sandboxRoot);
        Directory.CreateDirectory(workingDirectory);
        var sandboxEnvironment = runtime.CreateSandboxEnvironment(sandboxRoot);

        var result = await crawler.CrawlAsync(
            "demo",
            workingDirectory: workingDirectory,
            environment: sandboxEnvironment.Values,
            timeoutSeconds: 30,
            sandboxCleanupRoot: Path.GetFullPath(sandboxRoot),
            cancellationToken: CancellationToken.None);

        Assert.True(result.Documents.ContainsKey(string.Empty));
        Assert.Equal(2, runtime.Invocations.Count);
        Assert.Equal("--help", runtime.Invocations[0].Arguments[0]);
        Assert.Equal("--help", runtime.Invocations[1].Arguments[0]);
        Assert.False(runtime.Invocations[0].Environment.ContainsKey(DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName));
        Assert.Equal(
            DotnetRuntimeCompatibilitySupport.DotnetRollForwardMajorValue,
            runtime.Invocations[1].Environment[DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName]);
        Assert.All(runtime.Invocations, invocation =>
        {
            Assert.Equal(workingDirectory, invocation.WorkingDirectory);
            Assert.Equal(Path.GetFullPath(sandboxRoot), invocation.SandboxRoot);
            Assert.NotEqual(invocation.WorkingDirectory, invocation.SandboxRoot);
        });
        Assert.Equal("--help", result.CaptureSummaries[string.Empty].HelpSwitch);
    }

    [Fact]
    public async Task CrawlAsync_Fails_When_A_CliFx_Command_Exceeds_The_Child_Command_Budget()
    {
        var commandRows = Enumerable.Range(1, HelpCrawlGuardrailSupport.MaxChildCommandsPerDocument + 1)
            .Select(index => $"  cmd{index:D2}         Command {index}.")
            .ToArray();
        var runtime = new FanoutCommandRuntime(
            """
            demo

            USAGE
              demo [command]

            COMMANDS
            """ + Environment.NewLine + string.Join(Environment.NewLine, commandRows));
        var crawler = new CliFxHelpCrawler(runtime);

        var result = await crawler.CrawlAsync(
            "demo",
            workingDirectory: Environment.CurrentDirectory,
            environment: new Dictionary<string, string>(),
            timeoutSeconds: 30,
            sandboxCleanupRoot: null,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result.GuardrailFailureMessage);
        Assert.Contains(HelpCrawlGuardrailSupport.MaxChildCommandsPerDocument.ToString(), result.GuardrailFailureMessage);
        Assert.Equal([""], result.Documents.Keys);
        Assert.Null(runtime.LastSandboxRoot);
    }

    [Fact]
    public async Task CrawlAsync_Normalizes_Root_Qualified_Command_Entries()
    {
        var runtime = new RootQualifiedCommandRuntime();
        var crawler = new CliFxHelpCrawler(runtime);

        var result = await crawler.CrawlAsync(
            "demo",
            workingDirectory: Environment.CurrentDirectory,
            environment: new Dictionary<string, string>(),
            timeoutSeconds: 30,
            sandboxCleanupRoot: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(
            [
                "--help",
                "child --help",
            ],
            runtime.Invocations);
        Assert.Contains("child", result.Documents.Keys);
        Assert.DoesNotContain("demo child", result.Documents.Keys, StringComparer.OrdinalIgnoreCase);
    }

    private static CommandRuntime.ProcessResult MissingFrameworkResult()
        => new(
            Status: "failed",
            TimedOut: false,
            ExitCode: -2147450730,
            DurationMs: 1,
            Stdout: string.Empty,
            Stderr:
            """
            You must install or update .NET to run this application.

            Framework: 'Microsoft.NETCore.App', version '9.0.0' (x64)
            The following frameworks were found:
              10.0.5 at [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
            """);

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

    private sealed class FakeCommandRuntime : CommandRuntime
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
            var invocation = new InvocationRecord(
                argumentList.ToArray(),
                new Dictionary<string, string>(environment, StringComparer.OrdinalIgnoreCase),
                workingDirectory,
                sandboxRoot);
            Invocations.Add(invocation);

            var result = invocation.Environment.ContainsKey(DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName)
                ? HelpResult()
                : MissingFrameworkResult();
            return Task.FromResult(result);
        }
    }

    private sealed class FanoutCommandRuntime(string helpText) : CommandRuntime
    {
        public string? LastSandboxRoot { get; private set; }

        public override Task<ProcessResult> InvokeProcessCaptureAsync(
            string filePath,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environment,
            int timeoutSeconds,
            string? sandboxRoot,
            CancellationToken cancellationToken)
        {
            LastSandboxRoot = sandboxRoot;
            return Task.FromResult(new ProcessResult(
                Status: "ok",
                TimedOut: false,
                ExitCode: 0,
                DurationMs: 1,
                Stdout: helpText,
                Stderr: string.Empty));
        }
    }

    private sealed class RootQualifiedCommandRuntime : CommandRuntime
    {
        public List<string> Invocations { get; } = [];

        public override Task<ProcessResult> InvokeProcessCaptureAsync(
            string filePath,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environment,
            int timeoutSeconds,
            string? sandboxRoot,
            CancellationToken cancellationToken)
        {
            var invocation = string.Join(' ', argumentList);
            Invocations.Add(invocation);

            return Task.FromResult(new ProcessResult(
                Status: "ok",
                TimedOut: false,
                ExitCode: 0,
                DurationMs: 1,
                Stdout: string.Equals(invocation, "--help", StringComparison.Ordinal)
                    ? """
                      demo

                      USAGE
                        demo [command]

                      COMMANDS
                        demo child    Child command.
                      """
                    : """
                      demo child

                      USAGE
                        demo child [options]

                      OPTIONS
                        --verbose  Verbose output.
                      """,
                Stderr: string.Empty));
        }
    }

    private sealed record InvocationRecord(
        string[] Arguments,
        IReadOnlyDictionary<string, string> Environment,
        string WorkingDirectory,
        string? SandboxRoot);
}
