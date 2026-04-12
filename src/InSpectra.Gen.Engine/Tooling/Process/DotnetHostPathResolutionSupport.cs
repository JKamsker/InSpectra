namespace InSpectra.Gen.Engine.Tooling.Process;

using System.Runtime.InteropServices;

internal static class DotnetHostPathResolutionSupport
{
    public static string ResolveDotnetHostPath()
        => TryResolveDotnetHostPath() ?? "dotnet";

    public static string? TryResolveDotnetHostPath()
    {
        foreach (var candidate in EnumerateCandidateHostPaths())
        {
            if (TryNormalizeExistingFile(candidate) is { } resolvedPath)
            {
                return resolvedPath;
            }
        }

        return null;
    }

    public static string? TryResolveDotnetRootDirectory(string? dotnetHostPath)
    {
        if (string.IsNullOrWhiteSpace(dotnetHostPath))
        {
            return null;
        }

        try
        {
            var fullPath = Path.GetFullPath(dotnetHostPath);
            var resolvedHostPath = TryResolveFinalHostPath(fullPath) ?? fullPath;
            var hostDirectory = Path.GetDirectoryName(resolvedHostPath);
            if (string.IsNullOrWhiteSpace(hostDirectory))
            {
                return null;
            }

            for (var current = new DirectoryInfo(hostDirectory); current is not null; current = current.Parent)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "shared")))
                {
                    return current.FullName;
                }
            }

            return hostDirectory;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> EnumerateCandidateHostPaths()
    {
        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var seen = new HashSet<string>(comparer);

        var hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(hostPath) && seen.Add(hostPath))
        {
            yield return hostPath;
        }

        foreach (var variableName in GetPreferredDotnetRootVariables())
        {
            var dotnetRoot = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(dotnetRoot))
            {
                continue;
            }

            var candidatePath = Path.Combine(dotnetRoot, GetDotnetHostFileName());
            if (seen.Add(candidatePath))
            {
                yield return candidatePath;
            }
        }

        if (TryResolveFromPath() is { } pathResolvedHost && seen.Add(pathResolvedHost))
        {
            yield return pathResolvedHost;
        }

        foreach (var defaultRoot in EnumerateDefaultDotnetRoots())
        {
            var candidatePath = Path.Combine(defaultRoot, GetDotnetHostFileName());
            if (seen.Add(candidatePath))
            {
                yield return candidatePath;
            }
        }
    }

    private static IEnumerable<string> GetPreferredDotnetRootVariables()
    {
        if (OperatingSystem.IsWindows())
        {
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X64:
                    yield return "DOTNET_ROOT_X64";
                    break;
                case Architecture.X86:
                    yield return "DOTNET_ROOT_X86";
                    if (Environment.Is64BitOperatingSystem)
                    {
                        yield return "DOTNET_ROOT(x86)";
                    }

                    break;
                case Architecture.Arm64:
                    yield return "DOTNET_ROOT_ARM64";
                    break;
            }
        }

        yield return "DOTNET_ROOT";
    }

    private static IEnumerable<string> EnumerateDefaultDotnetRoots()
    {
        if (OperatingSystem.IsWindows())
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            if (RuntimeInformation.OSArchitecture == Architecture.Arm64
                && RuntimeInformation.ProcessArchitecture == Architecture.X64
                && !string.IsNullOrWhiteSpace(programFiles))
            {
                yield return Path.Combine(programFiles, "dotnet", "x64");
            }

            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                yield return Path.Combine(programFiles, "dotnet");
            }

            if (!string.IsNullOrWhiteSpace(programFilesX86))
            {
                yield return Path.Combine(programFilesX86, "dotnet");
            }

            yield break;
        }

        if (OperatingSystem.IsMacOS())
        {
            if (RuntimeInformation.OSArchitecture == Architecture.Arm64
                && RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                yield return "/usr/local/share/dotnet/x64";
            }

            yield return "/usr/local/share/dotnet";
            yield return "/opt/homebrew/share/dotnet";
            yield break;
        }

        yield return "/usr/share/dotnet";
        yield return "/usr/lib/dotnet";
        yield return "/usr/local/share/dotnet";
    }

    private static string? TryResolveFromPath()
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var segment in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidateDirectory = segment.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(candidateDirectory))
            {
                continue;
            }

            var candidatePath = Path.Combine(candidateDirectory, GetDotnetHostFileName());
            if (TryNormalizeExistingFile(candidatePath) is { } resolvedPath)
            {
                return resolvedPath;
            }
        }

        return null;
    }

    private static string? TryNormalizeExistingFile(string candidatePath)
    {
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            return null;
        }

        try
        {
            var fullPath = Path.GetFullPath(candidatePath);
            return File.Exists(fullPath) ? fullPath : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryResolveFinalHostPath(string fullPath)
    {
        try
        {
            var resolvedTarget = new FileInfo(fullPath).ResolveLinkTarget(returnFinalTarget: true);
            return resolvedTarget?.FullName;
        }
        catch
        {
            return null;
        }
    }

    private static string GetDotnetHostFileName()
        => OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
}
