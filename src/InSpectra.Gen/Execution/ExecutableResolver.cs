using InSpectra.Gen.Acquisition.Contracts.Exceptions;

namespace InSpectra.Gen.Execution;

public sealed class ExecutableResolver
{
    public string Resolve(string source, string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new CliUsageException("A source executable is required.");
        }

        if (ContainsDirectorySeparator(source) || Path.IsPathRooted(source))
        {
            var fullPath = Path.GetFullPath(source, workingDirectory);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            throw new CliSourceExecutionException($"Executable `{fullPath}` was not found.", "source_not_found");
        }

        var localMatch = ResolveFromDirectory(workingDirectory, source);
        if (localMatch is not null)
        {
            return localMatch;
        }

        foreach (var pathEntry in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var match = ResolveFromDirectory(pathEntry, source);
            if (match is not null)
            {
                return match;
            }
        }

        throw new CliSourceExecutionException($"Executable `{source}` could not be resolved from the working directory or PATH.", "source_not_found");
    }

    private static string? ResolveFromDirectory(string directory, string source)
    {
        if (!Directory.Exists(directory))
        {
            return null;
        }

        var exactPath = Path.Combine(directory, source);
        if (Path.HasExtension(source))
        {
            if (File.Exists(exactPath))
            {
                return exactPath;
            }

            return null;
        }

        if (OperatingSystem.IsWindows())
        {
            var pathExt = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var extension in pathExt)
            {
                var candidate = exactPath + extension.ToLowerInvariant();
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                candidate = exactPath + extension.ToUpperInvariant();
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        if (File.Exists(exactPath))
        {
            return exactPath;
        }

        return null;
    }

    private static bool ContainsDirectorySeparator(string value)
    {
        return value.Contains(Path.DirectorySeparatorChar) || value.Contains(Path.AltDirectorySeparatorChar);
    }
}
