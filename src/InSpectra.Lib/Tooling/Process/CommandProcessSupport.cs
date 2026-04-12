namespace InSpectra.Lib.Tooling.Process;

using System.Diagnostics;
using System.Text.RegularExpressions;

internal static partial class CommandProcessSupport
{
    private static readonly TimeSpan ExitGracePeriod = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan OutputDrainGracePeriod = TimeSpan.FromSeconds(1);

    public static async Task<CommandRuntime.ProcessResult> InvokeProcessCaptureAsync(
        Process process,
        int timeoutSeconds,
        string? sandboxRoot,
        CancellationToken cancellationToken,
        Action<string?> terminateSandboxProcesses)
    {
        using var ownedProcess = process;
        using var readerCancellation = new CancellationTokenSource();
        var stdout = ProcessOutputCaptureSupport.CreateBuffer();
        var stderr = ProcessOutputCaptureSupport.CreateBuffer();
        var sandboxCleanupRequested = false;
        var stopwatch = Stopwatch.StartNew();
        ownedProcess.Start();

        var stdoutTask = PumpStreamAsync(ownedProcess.StandardOutput, stdout, readerCancellation.Token);
        var stderrTask = PumpStreamAsync(ownedProcess.StandardError, stderr, readerCancellation.Token);
        var waitTask = ownedProcess.WaitForExitAsync(cancellationToken);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

        void TerminateSandboxProcessesOnce()
        {
            if (sandboxCleanupRequested)
            {
                return;
            }

            sandboxCleanupRequested = true;
            terminateSandboxProcesses(sandboxRoot);
        }

        try
        {
            var completedTask = await Task.WhenAny(waitTask, timeoutTask);
            var timedOut = completedTask == timeoutTask;
            if (timedOut)
            {
                TryKillProcess(ownedProcess);
                await WaitForExitAsync(ownedProcess, ExitGracePeriod);
                TerminateSandboxProcessesOnce();
            }
            else
            {
                await waitTask;
            }

            var drained = await TryWaitForCompletionAsync(stdoutTask, stderrTask, OutputDrainGracePeriod);
            if (!drained)
            {
                timedOut = true;
                TerminateSandboxProcessesOnce();
                readerCancellation.Cancel();
                await TryWaitForCompletionAsync(stdoutTask, stderrTask, OutputDrainGracePeriod);
            }

            stopwatch.Stop();
            return new CommandRuntime.ProcessResult(
                Status: timedOut ? "timed-out" : ownedProcess.ExitCode == 0 ? "ok" : "failed",
                TimedOut: timedOut,
                ExitCode: timedOut ? null : ownedProcess.ExitCode,
                DurationMs: (int)Math.Round(stopwatch.Elapsed.TotalMilliseconds),
                Stdout: stdout.ToString(),
                Stderr: stderr.ToString(),
                OutputLimitExceeded: stdout.LimitExceeded || stderr.LimitExceeded);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryKillProcess(ownedProcess);
            TerminateSandboxProcessesOnce();
            readerCancellation.Cancel();
            await TryWaitForCompletionAsync(stdoutTask, stderrTask, OutputDrainGracePeriod);
            throw;
        }
    }

    public static void TerminateSandboxProcesses(string? sandboxRoot)
    {
        if (string.IsNullOrWhiteSpace(sandboxRoot))
        {
            return;
        }

        var sandboxPath = SandboxProcessMatchSupport.NormalizeDirectoryPath(sandboxRoot);
        foreach (var candidate in Process.GetProcesses())
        {
            using (candidate)
            {
                if (candidate.Id == Environment.ProcessId)
                {
                    continue;
                }

                var executablePath = TryGetExecutablePath(candidate);
                if (executablePath is null
                    || !SandboxProcessMatchSupport.MatchesSandboxProcess(sandboxPath, executablePath, candidate))
                {
                    continue;
                }

                TryKillProcess(candidate);
            }
        }
    }

    internal static bool IsWithinSandboxRoot(string sandboxRoot, string executablePath)
        => SandboxProcessMatchSupport.IsWithinSandboxRoot(sandboxRoot, executablePath);

    internal static bool MatchesSandboxProcess(string sandboxRoot, string executablePath, string? commandLine)
        => SandboxProcessMatchSupport.MatchesSandboxProcess(sandboxRoot, executablePath, commandLine);

    public static string? ResolveInstalledCommandPath(string installDirectory, string commandName)
    {
        var candidates = new List<string>
        {
            Path.Combine(installDirectory, commandName),
            Path.Combine(installDirectory, commandName + ".exe"),
            Path.Combine(installDirectory, commandName + ".cmd"),
            Path.Combine(installDirectory, commandName + ".bat"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public static string? NormalizeConsoleText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var normalized = value.TrimStart('\uFEFF').Replace("\0", string.Empty, StringComparison.Ordinal);
        normalized = AnsiCsiRegex().Replace(normalized, string.Empty);
        normalized = AnsiEscapeRegex().Replace(normalized, string.Empty);
        normalized = normalized.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static async Task PumpStreamAsync(
        StreamReader reader,
        ProcessOutputCaptureSupport.LimitedOutputBuffer buffer,
        CancellationToken cancellationToken)
    {
        var chunk = new char[4096];
        while (true)
        {
            int readCount;
            try
            {
                readCount = await reader.ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (readCount == 0)
            {
                return;
            }

            buffer.Append(chunk, readCount);
        }
    }

    private static async Task<bool> TryWaitForCompletionAsync(Task stdoutTask, Task stderrTask, TimeSpan timeout)
    {
        var combinedTask = Task.WhenAll(stdoutTask, stderrTask);
        return await Task.WhenAny(combinedTask, Task.Delay(timeout)) == combinedTask;
    }

    private static async Task WaitForExitAsync(Process process, TimeSpan timeout)
    {
        if (process.HasExited)
        {
            return;
        }

        await Task.WhenAny(process.WaitForExitAsync(CancellationToken.None), Task.Delay(timeout));
    }

    private static string? TryGetExecutablePath(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return null;
            }

            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }

    [GeneratedRegex(@"\x1B\[[0-?]*[ -/]*[@-~]", RegexOptions.Compiled)]
    private static partial Regex AnsiCsiRegex();

    [GeneratedRegex(@"\x1B[@-_]", RegexOptions.Compiled)]
    private static partial Regex AnsiEscapeRegex();
}
