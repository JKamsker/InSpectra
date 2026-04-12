using System.Diagnostics;
using InSpectra.Gen.Core;

namespace InSpectra.Gen.Engine.Rendering.Html.Bundle;

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
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);

            await process.WaitForExitAsync(timeout.Token);
            _ = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                var details = new List<string> { $"Exit code: {process.ExitCode}" };
                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    details.Add(stderr.Trim());
                }

                throw CreateBuildFailure(workingDirectory, repositoryDist, $"`{executablePath}` exited with code {process.ExitCode}.", details);
            }
        }
        catch (OperationCanceledException)
        {
            TryTerminate(process);
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            throw CreateBuildFailure(
                workingDirectory,
                repositoryDist,
                $"`{executablePath}` did not finish within {timeoutSeconds} seconds.",
                arguments.Count > 0 ? [$"Arguments: {string.Join(' ', arguments)}"] : []);
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
