using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Rendering.Html.Bundle;

namespace InSpectra.Gen.Tests.Rendering;

public sealed class ViewerBundleProcessSupportTests
{
    [Fact]
    public async Task RunProcessAsync_OnTimeout_Preserves_Stdout_And_Stderr_In_Exception_Details()
    {
        var exception = await Assert.ThrowsAsync<CliUsageException>(() => ViewerBundleProcessSupport.RunProcessAsync(
            executablePath: GetShellExecutablePath(),
            workingDirectory: Environment.CurrentDirectory,
            repositoryDist: "expected-dist",
            arguments: GetTimeoutWithOutputArguments(),
            timeoutSeconds: 1,
            cancellationToken: CancellationToken.None));

        Assert.Contains(exception.Details, detail => detail.Contains("did not finish within 1 seconds", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("Arguments:", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("before out", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("before err", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunProcessAsync_OnNonZeroExit_Preserves_Stdout_And_Stderr_In_Exception_Details()
    {
        var exception = await Assert.ThrowsAsync<CliUsageException>(() => ViewerBundleProcessSupport.RunProcessAsync(
            executablePath: GetShellExecutablePath(),
            workingDirectory: Environment.CurrentDirectory,
            repositoryDist: "expected-dist",
            arguments: GetFailingArguments(),
            timeoutSeconds: 5,
            cancellationToken: CancellationToken.None));

        Assert.Contains(exception.Details, detail => detail.Contains("exited with code 7", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("Exit code: 7", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("before out", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("before err", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunProcessAsync_When_Process_Already_Exited_Does_Not_Report_A_Timeout()
    {
        var exception = await Assert.ThrowsAsync<CliUsageException>(() => ViewerBundleProcessSupport.RunProcessAsync(
            executablePath: GetExitedProcessExecutablePath(),
            workingDirectory: Environment.CurrentDirectory,
            repositoryDist: "expected-dist",
            arguments: GetExitedProcessWithOpenPipeArguments(),
            timeoutSeconds: 1,
            cancellationToken: CancellationToken.None));

        Assert.DoesNotContain(exception.Details, detail => detail.Contains("did not finish within", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("exited with code 7", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("Exit code: 7", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("before out", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("before err", StringComparison.Ordinal));
    }

    private static string GetShellExecutablePath()
        => OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh";

    private static IReadOnlyList<string> GetTimeoutWithOutputArguments()
        => OperatingSystem.IsWindows()
            ? ["/c", "(echo before out & echo before err 1>&2 & ping 127.0.0.1 -n 6 >nul)"]
            : ["-c", "printf 'before out\\n'; printf 'before err\\n' >&2; sleep 5"];

    private static IReadOnlyList<string> GetFailingArguments()
        => OperatingSystem.IsWindows()
            ? ["/c", "(echo before out & echo before err 1>&2 & exit /b 7)"]
            : ["-c", "printf 'before out\\n'; printf 'before err\\n' >&2; exit 7"];

    private static IReadOnlyList<string> GetExitedProcessWithOpenPipeArguments()
        => OperatingSystem.IsWindows()
            ? [
                "-NoProfile",
                "-Command",
                "[Console]::Out.WriteLine('before out'); " +
                "[Console]::Error.WriteLine('before err'); " +
                "$psi = New-Object System.Diagnostics.ProcessStartInfo 'powershell.exe', '-NoProfile -Command Start-Sleep -Seconds 2'; " +
                "$psi.UseShellExecute = $false; " +
                "$child = [System.Diagnostics.Process]::Start($psi); " +
                "if ($null -eq $child) { exit 9 }; " +
                "exit 7",
            ]
            : ["-c", "printf 'before out\\n'; printf 'before err\\n' >&2; (sleep 2) & exit 7"];

    private static string GetExitedProcessExecutablePath()
        => OperatingSystem.IsWindows() ? "powershell.exe" : GetShellExecutablePath();
}
