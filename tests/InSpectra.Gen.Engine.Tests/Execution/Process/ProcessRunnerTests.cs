namespace InSpectra.Gen.Engine.Tests.Execution.Process;

using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Execution.Process;

public sealed class ProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_OnTimeout_Terminates_Sandbox_Processes_With_Cleanup_Root()
    {
        var cleanupRoots = new List<string?>();
        var runner = new ProcessRunner(cleanupRoots.Add);

        var exception = await Assert.ThrowsAsync<CliSourceExecutionException>(() => runner.RunAsync(
            executablePath: GetLongRunningExecutablePath(),
            workingDirectory: Environment.CurrentDirectory,
            arguments: GetLongRunningArguments(),
            timeoutSeconds: 1,
            environment: null,
            cleanupRoot: "sandbox-root",
            cancellationToken: CancellationToken.None));

        Assert.Contains("did not finish within 1 seconds", exception.Message, StringComparison.Ordinal);
        Assert.Equal(["sandbox-root"], cleanupRoots);
    }

    private static string GetLongRunningExecutablePath()
        => OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh";

    private static IReadOnlyList<string> GetLongRunningArguments()
        => OperatingSystem.IsWindows()
            ? ["/c", "ping 127.0.0.1 -n 6 >nul"]
            : ["-c", "sleep 5"];
}
