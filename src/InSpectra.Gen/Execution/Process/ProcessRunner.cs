using System.Diagnostics;
using InSpectra.Gen.Core;

namespace InSpectra.Gen.Execution.Process;

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => await RunAsync(executablePath, workingDirectory, arguments, timeoutSeconds, environment: null, cancellationToken);

    public async Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken)
    {
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

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);

            await process.WaitForExitAsync(timeout.Token);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                var details = new List<string> { $"Exit code: {process.ExitCode}" };
                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    details.Add(stderr.Trim());
                }

                throw new CliSourceExecutionException($"`{executablePath}` exited with code {process.ExitCode}.", details: details);
            }

            return new ProcessResult(stdout, stderr);
        }
        catch (OperationCanceledException exception)
        {
            TryTerminate(process);
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            throw new CliSourceExecutionException(
                $"`{executablePath}` did not finish within {timeoutSeconds} seconds.",
                details: arguments.Count > 0 ? [$"Arguments: {string.Join(' ', arguments)}"] : [],
                innerException: exception);
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
