namespace InSpectra.Gen.Engine.Tests.Execution.Process;

using System.Diagnostics;
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

    [Fact]
    public async Task RunAsync_OnTimeout_Preserves_Captured_Output_In_Exception_Details()
    {
        var runner = new ProcessRunner(_ => { });

        var exception = await Assert.ThrowsAsync<CliSourceExecutionException>(() => runner.RunAsync(
            executablePath: GetShellExecutablePath(),
            workingDirectory: Environment.CurrentDirectory,
            arguments: GetTimeoutWithOutputArguments(),
            timeoutSeconds: 1,
            environment: null,
            cleanupRoot: "sandbox-root",
            cancellationToken: CancellationToken.None));

        Assert.Contains(exception.Details, detail => detail.Contains("Arguments:", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("before out", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("before err", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_OnNonZeroExit_Preserves_Stdout_And_Stderr_In_Exception_Details()
    {
        var runner = new ProcessRunner(_ => { });

        var exception = await Assert.ThrowsAsync<CliSourceExecutionException>(() => runner.RunAsync(
            executablePath: GetShellExecutablePath(),
            workingDirectory: Environment.CurrentDirectory,
            arguments: GetFailingArguments(),
            timeoutSeconds: 5,
            environment: null,
            cleanupRoot: null,
            cancellationToken: CancellationToken.None));

        Assert.Contains(exception.Details, detail => detail.Contains("Exit code: 7", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("before out", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("before err", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_WhenCancellationRacesTimeout_PrefersCallerCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var runner = new ProcessRunner(_ => { });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.RunAsync(
            executablePath: GetShellExecutablePath(),
            workingDirectory: Environment.CurrentDirectory,
            arguments: GetLongRunningArguments(),
            timeoutSeconds: 0,
            environment: null,
            cleanupRoot: "sandbox-root",
            cancellationToken: cancellationSource.Token));
    }

    [Fact]
    public async Task RunAsync_OnCallerCancellation_ReturnsPromptly()
    {
        using var cancellationSource = new CancellationTokenSource();
        var runner = new ProcessRunner(_ => { });
        var stopwatch = Stopwatch.StartNew();

        cancellationSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.RunAsync(
            executablePath: GetShellExecutablePath(),
            workingDirectory: Environment.CurrentDirectory,
            arguments: GetLongRunningArguments(),
            timeoutSeconds: 10,
            environment: null,
            cleanupRoot: "sandbox-root",
            cancellationToken: cancellationSource.Token));

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), $"Cancellation took {stopwatch.Elapsed}.");
    }

    private static string GetLongRunningExecutablePath()
        => GetShellExecutablePath();

    private static string GetShellExecutablePath()
        => OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh";

    private static IReadOnlyList<string> GetLongRunningArguments()
        => OperatingSystem.IsWindows()
            ? ["/c", "ping 127.0.0.1 -n 6 >nul"]
            : ["-c", "sleep 5"];

    private static IReadOnlyList<string> GetTimeoutWithOutputArguments()
        => OperatingSystem.IsWindows()
            ? ["/c", "(echo before out & echo before err 1>&2 & ping 127.0.0.1 -n 6 >nul)"]
            : ["-c", "printf 'before out\\n'; printf 'before err\\n' >&2; sleep 5"];

    private static IReadOnlyList<string> GetFailingArguments()
        => OperatingSystem.IsWindows()
            ? ["/c", "(echo before out & echo before err 1>&2 & exit /b 7)"]
            : ["-c", "printf 'before out\\n'; printf 'before err\\n' >&2; exit 7"];
}
