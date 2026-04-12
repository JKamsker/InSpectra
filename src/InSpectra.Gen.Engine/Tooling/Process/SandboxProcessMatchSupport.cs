namespace InSpectra.Gen.Engine.Tooling.Process;

using System.Diagnostics;
using System.Text;

internal static class SandboxProcessMatchSupport
{
    public static string NormalizeDirectoryPath(string path)
        => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    public static bool IsWithinSandboxRoot(string sandboxRoot, string executablePath)
    {
        var normalizedSandboxRoot = NormalizeDirectoryPath(sandboxRoot);
        var normalizedExecutablePath = Path.GetFullPath(executablePath);
        return !IsFilesystemRoot(normalizedSandboxRoot)
            && PathContainsSandboxRoot(normalizedExecutablePath, normalizedSandboxRoot);
    }

    public static bool MatchesSandboxProcess(string sandboxRoot, string executablePath, Process process)
        => MatchesSandboxProcess(
            sandboxRoot,
            executablePath,
            IsDotnetHost(executablePath) ? TryGetCommandLine(process) : null);

    public static bool MatchesSandboxProcess(string sandboxRoot, string executablePath, string? commandLine)
    {
        var normalizedSandboxRoot = NormalizeDirectoryPath(sandboxRoot);
        if (IsFilesystemRoot(normalizedSandboxRoot))
        {
            return false;
        }

        var normalizedExecutablePath = Path.GetFullPath(executablePath);
        return PathContainsSandboxRoot(normalizedExecutablePath, normalizedSandboxRoot)
            || (IsDotnetHost(normalizedExecutablePath)
                && CommandLineContainsSandboxRoot(commandLine, normalizedSandboxRoot));
    }

    private static string? TryGetCommandLine(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return null;
            }

            return OperatingSystem.IsWindows()
                ? TryGetWindowsCommandLine(process.Id)
                : TryGetProcfsCommandLine(process.Id);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsFilesystemRoot(string path)
    {
        var pathRoot = Path.GetPathRoot(path);
        return !string.IsNullOrWhiteSpace(pathRoot)
            && string.Equals(NormalizeDirectoryPath(pathRoot), path, GetPathComparison());
    }

    private static bool PathContainsSandboxRoot(string candidatePath, string sandboxRoot)
    {
        var comparison = GetPathComparison();
        return candidatePath.StartsWith(
                sandboxRoot + Path.DirectorySeparatorChar,
                comparison)
            || candidatePath.StartsWith(
                sandboxRoot + Path.AltDirectorySeparatorChar,
                comparison);
    }

    private static bool CommandLineContainsSandboxRoot(string? commandLine, string sandboxRoot)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return false;
        }

        var comparison = GetPathComparison();
        return commandLine.Contains(sandboxRoot + Path.DirectorySeparatorChar, comparison)
            || commandLine.Contains(sandboxRoot + Path.AltDirectorySeparatorChar, comparison);
    }

    private static bool IsDotnetHost(string executablePath)
        => string.Equals(
            Path.GetFileNameWithoutExtension(executablePath),
            "dotnet",
            GetPathComparison());

    private static string? TryGetWindowsCommandLine(int processId)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(
            $"$process = Get-CimInstance Win32_Process -Filter \"ProcessId = {processId}\"; if ($process) {{ $process.CommandLine }}");

        using var commandProcess = Process.Start(startInfo);
        if (commandProcess is null)
        {
            return null;
        }

        var outputTask = commandProcess.StandardOutput.ReadToEndAsync();
        if (!commandProcess.WaitForExit(milliseconds: 1000))
        {
            TryKillProcess(commandProcess);
            return null;
        }

        return CommandProcessSupport.NormalizeConsoleText(outputTask.GetAwaiter().GetResult());
    }

    private static string? TryGetProcfsCommandLine(int processId)
    {
        var procfsPath = $"/proc/{processId}/cmdline";
        if (!File.Exists(procfsPath))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(procfsPath);
        if (bytes.Length == 0)
        {
            return null;
        }

        return CommandProcessSupport.NormalizeConsoleText(Encoding.UTF8.GetString(bytes).Replace('\0', ' '));
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

    private static StringComparison GetPathComparison()
        => OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
