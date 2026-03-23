using System.Diagnostics;
using OpenCli.Renderer.Runtime;

namespace OpenCli.Renderer.Services;

public sealed class ProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
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

        using var process = new Process { StartInfo = startInfo };
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

                throw new CliSourceExecutionException($"`{executablePath}` failed while producing OpenCLI output.", details: details);
            }

            return new ProcessResult(stdout, stderr);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            TryTerminate(process);
            throw new CliSourceExecutionException(
                $"`{executablePath}` did not finish within {timeoutSeconds} seconds.",
                details: arguments.Count > 0 ? [$"Arguments: {string.Join(' ', arguments)}"] : [],
                innerException: exception);
        }
    }

    private static void TryTerminate(Process process)
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
}

public sealed record ProcessResult(string StandardOutput, string StandardError);
