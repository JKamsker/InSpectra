using System.Diagnostics;
using InSpectra.Lib;

namespace InSpectra.Lib.Rendering.Html.Bundle;

internal static class ViewerBundleProcessSupport
{
    public static string ResolveNpmExecutable(string frontendRoot, string? configuredExecutable, string frontendBuildHint)
    {
        if (!string.IsNullOrWhiteSpace(configuredExecutable))
        {
            return Path.IsPathRooted(configuredExecutable) || ContainsDirectorySeparator(configuredExecutable)
                ? Path.GetFullPath(configuredExecutable, frontendRoot)
                : configuredExecutable;
        }

        foreach (var directory in EnumerateSearchDirectories(frontendRoot))
        {
            var match = ResolveFromDirectory(directory, "npm");
            if (match is not null)
            {
                return match;
            }
        }

        throw new CliUsageException($"InSpectra.UI dist was not found and `npm` is not available on PATH. {frontendBuildHint}");
    }

    public static async Task RunProcessAsync(
        string executablePath,
        string workingDirectory,
        string repositoryDist,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = CreateStartInfo(executablePath, workingDirectory) };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        try
        {
            process.Start();
        }
        catch (Exception exception)
        {
            throw CreateBuildFailure(workingDirectory, repositoryDist, $"Failed to start `{executablePath}`.", [exception.Message]);
        }

        process.StandardInput.Close();
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var stopwatch = Stopwatch.StartNew();
        var stdoutCapture = new ViewerBundleStreamCapture(process.StandardOutput);
        var stderrCapture = new ViewerBundleStreamCapture(process.StandardError);

        try
        {
            await WaitForExitAsync(process, timeout, cancellationToken);
            var remainingDrainTime = RemainingDuration(timeout, stopwatch.Elapsed);
            var (stdout, stderr) = await ReadOutputAsync(
                stdoutCapture,
                stderrCapture,
                remainingDrainTime,
                cancellationToken);

            if (process.ExitCode != 0)
            {
                throw CreateBuildFailure(
                    workingDirectory,
                    repositoryDist,
                    $"`{executablePath}` exited with code {process.ExitCode}.",
                    CreateExitDetails(process.ExitCode, stdout, stderr));
            }
        }
        catch (OperationCanceledException)
        {
            TryTerminate(process);
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            stdoutCapture.ObserveFaults();
            stderrCapture.ObserveFaults();
            throw;
        }
        catch (TimeoutException)
        {
            TryTerminate(process);
            var stdout = stdoutCapture.GetLatestText();
            var stderr = stderrCapture.GetLatestText();
            stdoutCapture.ObserveFaults();
            stderrCapture.ObserveFaults();
            cancellationToken.ThrowIfCancellationRequested();
            throw CreateBuildFailure(
                workingDirectory,
                repositoryDist,
                $"`{executablePath}` did not finish within {timeoutSeconds} seconds.",
                CreateTimeoutDetails(arguments, stdout, stderr));
        }
    }

    private static IEnumerable<string> EnumerateSearchDirectories(string workingDirectory)
    {
        yield return workingDirectory;
        foreach (var pathEntry in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return pathEntry;
        }
    }

    private static string? ResolveFromDirectory(string directory, string executableName)
    {
        if (!Directory.Exists(directory))
        {
            return null;
        }

        var exactPath = Path.Combine(directory, executableName);
        if (Path.HasExtension(executableName))
        {
            return File.Exists(exactPath) ? exactPath : null;
        }

        if (OperatingSystem.IsWindows())
        {
            foreach (var extension in (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM")
                         .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (File.Exists(exactPath + extension.ToLowerInvariant()))
                {
                    return exactPath + extension.ToLowerInvariant();
                }

                if (File.Exists(exactPath + extension.ToUpperInvariant()))
                {
                    return exactPath + extension.ToUpperInvariant();
                }
            }
        }

        return File.Exists(exactPath) ? exactPath : null;
    }

    private static bool ContainsDirectorySeparator(string value)
        => value.Contains(Path.DirectorySeparatorChar) || value.Contains(Path.AltDirectorySeparatorChar);

    private static async Task WaitForExitAsync(
        Process process,
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
        ViewerBundleStreamCapture stdoutCapture,
        ViewerBundleStreamCapture stderrCapture,
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

    private static ProcessStartInfo CreateStartInfo(string executablePath, string workingDirectory)
        => new()
        {
            FileName = executablePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
        };

    private static CliUsageException CreateBuildFailure(
        string frontendRoot,
        string repositoryDist,
        string message,
        IReadOnlyList<string>? details = null)
        => new(
            $"Failed to build InSpectra.UI in `{frontendRoot}`.",
            [message, .. details ?? [], $"Expected bundle path: `{repositoryDist}`."]);

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
