namespace InSpectra.Lib.Tests.Tooling.Process;

using InSpectra.Lib.Tooling.Process;

public sealed class DotnetRuntimeCompatibilitySupportTests
{
    [Fact]
    public async Task InvokeWithCompatibilityRetriesAsync_Retries_When_RollForward_Reveals_Missing_Icu()
    {
        var runtime = new RecordingCommandRuntime(
            MissingSharedRuntimeResult(),
            MissingIcuResult(),
            SuccessResult());

        var result = await DotnetRuntimeCompatibilitySupport.InvokeWithCompatibilityRetriesAsync(
            runtime,
            filePath: "demo",
            argumentList: ["--help"],
            workingDirectory: Environment.CurrentDirectory,
            environment: new Dictionary<string, string>(),
            timeoutSeconds: 30,
            sandboxRoot: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(3, runtime.Invocations.Count);
        Assert.False(runtime.Invocations[0].ContainsKey(DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName));
        Assert.Equal(
            DotnetRuntimeCompatibilitySupport.DotnetRollForwardMajorValue,
            runtime.Invocations[1][DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName]);
        Assert.Equal("1", runtime.Invocations[2][DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName]);
        Assert.Equal(
            DotnetRuntimeCompatibilitySupport.DotnetRollForwardMajorValue,
            runtime.Invocations[2][DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName]);
        Assert.Equal("ok", result.Status);
    }

    [Fact]
    public async Task InvokeWithCompatibilityRetriesAsync_Retries_When_Invariant_Reveals_Missing_Shared_Runtime()
    {
        var runtime = new RecordingCommandRuntime(
            MissingIcuResult(),
            MissingSharedRuntimeResult(),
            SuccessResult());

        var result = await DotnetRuntimeCompatibilitySupport.InvokeWithCompatibilityRetriesAsync(
            runtime,
            filePath: "demo",
            argumentList: ["--help"],
            workingDirectory: Environment.CurrentDirectory,
            environment: new Dictionary<string, string>(),
            timeoutSeconds: 30,
            sandboxRoot: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(3, runtime.Invocations.Count);
        Assert.False(runtime.Invocations[0].ContainsKey(DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName));
        Assert.Equal("1", runtime.Invocations[1][DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName]);
        Assert.Equal(
            DotnetRuntimeCompatibilitySupport.DotnetRollForwardMajorValue,
            runtime.Invocations[2][DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName]);
        Assert.Equal("ok", result.Status);
    }

    [Fact]
    public async Task InvokeWithCompatibilityRetriesAsync_Overrides_Preexisting_Invariant_Value()
    {
        var runtime = new RecordingCommandRuntime(
            MissingIcuResult(),
            SuccessResult());

        var result = await DotnetRuntimeCompatibilitySupport.InvokeWithCompatibilityRetriesAsync(
            runtime,
            filePath: "demo",
            argumentList: ["--help"],
            workingDirectory: Environment.CurrentDirectory,
            environment: new Dictionary<string, string>
            {
                [DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName] = "0",
            },
            timeoutSeconds: 30,
            sandboxRoot: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(2, runtime.Invocations.Count);
        Assert.Equal("0", runtime.Invocations[0][DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName]);
        Assert.Equal("1", runtime.Invocations[1][DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName]);
        Assert.Equal("ok", result.Status);
    }

    [Fact]
    public async Task InvokeWithCompatibilityRetriesAsync_Overrides_Preexisting_RollForward_Value()
    {
        var runtime = new RecordingCommandRuntime(
            MissingSharedRuntimeResult(),
            SuccessResult());

        var result = await DotnetRuntimeCompatibilitySupport.InvokeWithCompatibilityRetriesAsync(
            runtime,
            filePath: "demo",
            argumentList: ["--help"],
            workingDirectory: Environment.CurrentDirectory,
            environment: new Dictionary<string, string>
            {
                [DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName] = "Minor",
            },
            timeoutSeconds: 30,
            sandboxRoot: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(2, runtime.Invocations.Count);
        Assert.Equal("Minor", runtime.Invocations[0][DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName]);
        Assert.Equal(
            DotnetRuntimeCompatibilitySupport.DotnetRollForwardMajorValue,
            runtime.Invocations[1][DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName]);
        Assert.Equal("ok", result.Status);
    }

    [Fact]
    public void DetectMissingFramework_Uses_Combined_Host_Output()
    {
        var issue = DotnetRuntimeCompatibilitySupport.DetectMissingFramework(
            command: "demo",
            stdout: "Framework: 'Microsoft.NETCore.App', version '9.0.0' (x64)",
            stderr: "The following frameworks were found:");

        Assert.NotNull(issue);
        Assert.Equal("demo", issue.Command);
        Assert.Equal("missing-framework", issue.Mode);
        Assert.NotNull(issue.Requirement);
        Assert.Equal("Microsoft.NETCore.App", issue.Requirement!.Name);
        Assert.Equal("9.0.0", issue.Requirement.Version);
    }

    private static CommandRuntime.ProcessResult MissingSharedRuntimeResult()
        => new(
            Status: "failed",
            TimedOut: false,
            ExitCode: 1,
            DurationMs: 1,
            Stdout: string.Empty,
            Stderr:
            """
            You must install or update .NET to run this application.

            Framework: 'Microsoft.NETCore.App', version '9.0.0' (x64)
            """);

    private static CommandRuntime.ProcessResult MissingIcuResult()
        => new(
            Status: "failed",
            TimedOut: false,
            ExitCode: 1,
            DurationMs: 1,
            Stdout: string.Empty,
            Stderr: "Couldn't find a valid ICU package installed on the system.");

    private static CommandRuntime.ProcessResult SuccessResult()
        => new(
            Status: "ok",
            TimedOut: false,
            ExitCode: 0,
            DurationMs: 1,
            Stdout: "help",
            Stderr: string.Empty);

    private sealed class RecordingCommandRuntime(params CommandRuntime.ProcessResult[] results) : CommandRuntime
    {
        private readonly Queue<CommandRuntime.ProcessResult> _results = new(results);

        public List<IReadOnlyDictionary<string, string>> Invocations { get; } = [];

        public override Task<ProcessResult> InvokeProcessCaptureAsync(
            string filePath,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environment,
            int timeoutSeconds,
            string? sandboxRoot,
            CancellationToken cancellationToken)
        {
            Invocations.Add(new Dictionary<string, string>(environment, StringComparer.OrdinalIgnoreCase));
            return Task.FromResult(_results.Dequeue());
        }
    }
}
