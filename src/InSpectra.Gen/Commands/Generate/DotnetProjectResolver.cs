using InSpectra.Lib;

namespace InSpectra.Gen.Commands.Generate;

/// <summary>
/// Resolves the &lt;PROJECT&gt; argument for `generate dotnet` commands to an
/// absolute path to a .NET project file that can be passed to
/// <c>dotnet run --project</c>.
/// </summary>
public static class DotnetProjectResolver
{
    private static readonly string[] ProjectExtensions = [".csproj", ".fsproj", ".vbproj"];

    public static string Resolve(string value, string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CliUsageException("A .NET project path is required.");
        }

        var candidate = Path.GetFullPath(value, workingDirectory);

        if (File.Exists(candidate))
        {
            if (!IsProjectFile(candidate))
            {
                throw new CliUsageException(
                    $"`{candidate}` is not a .NET project file (expected .csproj, .fsproj, or .vbproj).");
            }

            return candidate;
        }

        if (Directory.Exists(candidate))
        {
            var matches = ProjectExtensions
                .SelectMany(ext => Directory.EnumerateFiles(candidate, "*" + ext, SearchOption.TopDirectoryOnly))
                .OrderBy(static p => p, StringComparer.Ordinal)
                .ToArray();

            if (matches.Length == 0)
            {
                throw new CliUsageException(
                    $"No .NET project file (.csproj, .fsproj, .vbproj) was found in `{candidate}`.");
            }

            if (matches.Length > 1)
            {
                var names = string.Join(", ", matches.Select(Path.GetFileName));
                throw new CliUsageException(
                    $"Multiple .NET project files were found in `{candidate}`: {names}. Specify one explicitly.");
            }

            return matches[0];
        }

        throw new CliUsageException($"Project `{candidate}` was not found.");
    }

    private static bool IsProjectFile(string path)
    {
        var extension = Path.GetExtension(path);
        foreach (var known in ProjectExtensions)
        {
            if (string.Equals(extension, known, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
