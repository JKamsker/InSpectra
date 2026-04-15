namespace InSpectra.Lib.Tooling.Paths;

using InSpectra.Lib.Contracts;
using InSpectra.Lib.Tooling.Json;

using System.Text;

public static class RepositoryPathResolver
{
    public static string ResolveRepositoryRoot(string? explicitRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitRoot))
        {
            return Path.GetFullPath(explicitRoot);
        }

        var envRoot = Environment.GetEnvironmentVariable(InspectraProductInfo.RepositoryRootEnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(envRoot))
        {
            // Keep the old variable as a compatibility alias while the renamed repo shape settles.
            envRoot = Environment.GetEnvironmentVariable(InspectraProductInfo.LegacyRepositoryRootEnvironmentVariableName);
        }

        if (!string.IsNullOrWhiteSpace(envRoot))
        {
            return Path.GetFullPath(envRoot);
        }

        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            if (ContainsRepositoryMarker(current.FullName))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Environment.CurrentDirectory;
    }

    public static string GetRelativePath(string repositoryRoot, string path)
        => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');

    public static void EnsureParentDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public static void WriteJsonFile<T>(string path, T value)
    {
        EnsureParentDirectory(path);
        var json = System.Text.Json.JsonSerializer.Serialize(value, JsonOptions.RepositoryFiles);
        File.WriteAllText(path, json + Environment.NewLine, new UTF8Encoding(false));
    }

    public static void WriteTextFile(string path, string content)
    {
        EnsureParentDirectory(path);
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    public static void WriteLines(string path, IEnumerable<string> lines)
    {
        EnsureParentDirectory(path);
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    private static bool ContainsRepositoryMarker(string directoryPath)
    {
        foreach (var solutionFileName in InspectraProductInfo.RepositorySolutionFileNames)
        {
            if (File.Exists(Path.Combine(directoryPath, solutionFileName)))
            {
                return true;
            }
        }

        return false;
    }
}

