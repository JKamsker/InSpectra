using System.Diagnostics;
using System.Text;
using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Tooling.Process;

namespace InSpectra.Gen.Engine.Execution.Process;

internal sealed class ProcessRunner : IProcessRunner
{
    private readonly Action<string?> _terminateSandboxProcesses;

    public ProcessRunner()
        : this(CommandProcessSupport.TerminateSandboxProcesses)
    {
    }

    internal ProcessRunner(Action<string?> terminateSandboxProcesses)
    {
        _terminateSandboxProcesses = terminateSandboxProcesses;
    }

    public async Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => await RunAsync(
            executablePath,
            workingDirectory,
            arguments,
            timeoutSeconds,
            environment: null,
            cleanupRoot: null,
            cancellationToken);

    public async Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        IReadOnlyDictionary<string, string>? environment,
        string? cleanupRoot,
        CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var pair in environment)
            {
                startInfo.Environment[pair.Key] = pair.Value;
            }
        }

        using var process = new System.Diagnostics.Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Exception exception)
        {
            throw new CliSourceExecutionException($"Failed to start `{executablePath}`.", details: [exception.Message], innerException: exception);
        }

        process.StandardInput.Close();
        var stopwatch = Stopwatch.StartNew();
        var stdoutCapture = new StreamCapture(process.StandardOutput);
        var stderrCapture = new StreamCapture(process.StandardError);

        try
        {
            await WaitForProcessExitAsync(process, timeout, cancellationToken);
            var remainingDrainTime = RemainingDuration(timeout, stopwatch.Elapsed);
            var (stdout, stderr) = await ReadOutputAsync(
                stdoutCapture,
                stderrCapture,
                remainingDrainTime,
                cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new CliSourceExecutionException(
                    $"`{executablePath}` exited with code {process.ExitCode}.",
                    details: CreateExitDetails(process.ExitCode, stdout, stderr));
            }

            return new ProcessResult(stdout, stderr);
        }
        catch (OperationCanceledException)
        {
            CleanupAfterCancellation(process, cleanupRoot);
            stdoutCapture.ObserveFaults();
            stderrCapture.ObserveFaults();
            throw;
        }
        catch (TimeoutException exception)
        {
            CleanupAfterCancellation(process, cleanupRoot);
            var stdout = stdoutCapture.GetLatestText();
            var stderr = stderrCapture.GetLatestText();
            stdoutCapture.ObserveFaults();
            stderrCapture.ObserveFaults();
            cancellationToken.ThrowIfCancellationRequested();
            throw new CliSourceExecutionException(
                $"`{executablePath}` did not finish within {timeoutSeconds} seconds.",
                details: CreateTimeoutDetails(arguments, stdout, stderr),
                innerException: exception);
        }
    }

    private void CleanupAfterCancellation(
        System.Diagnostics.Process process,
        string? cleanupRoot)
    {
        TryTerminate(process);
        _terminateSandboxProcesses(cleanupRoot);
    }

    private static async Task WaitForProcessExitAsync(
        System.Diagnostics.Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var processExitTask = process.WaitForExitAsync(cancellationToken);
        var timeoutTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(processExitTask, timeoutTask);
        if (completedTask == processExitTask || processExitTask.IsCompleted)
        {
            await processExitTask;
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new TimeoutException();
    }

    private static TimeSpan RemainingDuration(TimeSpan timeout, TimeSpan elapsed)
        => timeout > elapsed ? timeout - elapsed : TimeSpan.Zero;

    private static async Task<(string StandardOutput, string StandardError)> ReadOutputAsync(
        StreamCapture stdoutCapture,
        StreamCapture stderrCapture,
        TimeSpan maxWait,
        CancellationToken cancellationToken)
    {
        var stdoutTask = stdoutCapture.GetTextAsync(maxWait, cancellationToken);
        var stderrTask = stderrCapture.GetTextAsync(maxWait, cancellationToken);
        await Task.WhenAll(stdoutTask, stderrTask);
        return (await stdoutTask, await stderrTask);
    }

    private static IReadOnlyList<string> CreateTimeoutDetails(
        IReadOnlyList<string> arguments,
        string stdout,
        string stderr)
    {
        var details = new List<string>();
        if (arguments.Count > 0)
        {
            details.Add($"Arguments: {string.Join(' ', arguments)}");
        }

        AddOutputDetail(details, "Standard output", stdout);
        AddOutputDetail(details, "Standard error", stderr);
        return details;
    }

    private static IReadOnlyList<string> CreateExitDetails(int exitCode, string stdout, string stderr)
    {
        var details = new List<string> { $"Exit code: {exitCode}" };
        AddOutputDetail(details, "Standard output", stdout);
        AddOutputDetail(details, "Standard error", stderr);
        return details;
    }

    private static void AddOutputDetail(List<string> details, string label, string output)
    {
        if (!string.IsNullOrWhiteSpace(output))
        {
            details.Add($"{label}:{Environment.NewLine}{output.Trim()}");
        }
    }

    private sealed class StreamCapture
    {
        private readonly object _sync = new();
        private readonly StringBuilder _buffer = new();

        public StreamCapture(StreamReader reader)
        {
            Completion = CaptureAsync(reader);
        }

        private Task<string> Completion { get; }

        public async Task<string> GetTextAsync(TimeSpan maxWait, CancellationToken cancellationToken)
        {
            if (Completion.IsCompletedSuccessfully)
            {
                return Completion.Result;
            }

            if (maxWait <= TimeSpan.Zero)
            {
                return Snapshot();
            }

            try
            {
                return await Completion.WaitAsync(maxWait, cancellationToken);
            }
            catch (TimeoutException)
            {
                return Snapshot();
            }
        }

        public string GetLatestText()
            => Completion.IsCompletedSuccessfully ? Completion.Result : Snapshot();

        public void ObserveFaults()
        {
            _ = Completion.ContinueWith(
                static task => _ = task.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private async Task<string> CaptureAsync(StreamReader reader)
        {
            var buffer = new char[4096];

            while (true)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length));
                if (read == 0)
                {
                    return Snapshot();
                }

                lock (_sync)
                {
                    _buffer.Append(buffer, 0, read);
                }
            }
        }

        private string Snapshot()
        {
            lock (_sync)
            {
                return _buffer.ToString();
            }
        }
    }

    private static void TryTerminate(System.Diagnostics.Process process)
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
            // Expected: process may have already exited (InvalidOperationException/Win32Exception)
        }
    }
}
