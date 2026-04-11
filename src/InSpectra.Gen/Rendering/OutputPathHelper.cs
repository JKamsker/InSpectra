using InSpectra.Gen.Acquisition.Contracts.Exceptions;

namespace InSpectra.Gen.Rendering;

public static class OutputPathHelper
{
    public static void EnsureFileWritable(string outputFile, bool overwrite)
    {
        var resolvedOutputFile = Path.GetFullPath(outputFile);
        if (Directory.Exists(resolvedOutputFile))
        {
            throw new CliUsageException($"Output file `{outputFile}` points to an existing directory.");
        }

        if (File.Exists(resolvedOutputFile) && !overwrite)
        {
            throw new CliUsageException($"Output file `{outputFile}` already exists. Use `--overwrite` to replace it.");
        }
    }

    public static void PrepareDirectory(string outputDirectory, bool overwrite)
    {
        var resolvedOutputDirectory = NormalizeDirectoryPath(outputDirectory);
        if (File.Exists(resolvedOutputDirectory))
        {
            throw new CliUsageException($"Output directory `{outputDirectory}` points to an existing file.");
        }

        if (!Directory.Exists(resolvedOutputDirectory))
        {
            Directory.CreateDirectory(resolvedOutputDirectory);
            return;
        }

        if (!Directory.EnumerateFileSystemEntries(resolvedOutputDirectory).Any())
        {
            return;
        }

        if (!overwrite)
        {
            throw new CliUsageException($"Output directory `{outputDirectory}` is not empty. Use `--overwrite` to replace it.");
        }

        EnsureDirectoryCanBeReplaced(resolvedOutputDirectory);
        ClearDirectoryContents(resolvedOutputDirectory);
    }

    private static void EnsureDirectoryCanBeReplaced(string outputDirectory)
    {
        var directoryRoot = Path.GetPathRoot(outputDirectory);
        if (!string.IsNullOrWhiteSpace(directoryRoot)
            && string.Equals(outputDirectory, NormalizeDirectoryPath(directoryRoot), StringComparison.OrdinalIgnoreCase))
        {
            throw new CliUsageException(
                $"Refusing to replace directory `{outputDirectory}` because it resolves to a filesystem root.");
        }

        var currentDirectory = NormalizeDirectoryPath(Environment.CurrentDirectory);
        if (IsSameOrAncestor(outputDirectory, currentDirectory))
        {
            throw new CliUsageException(
                $"Refusing to replace directory `{outputDirectory}` because it is the current working directory or one of its ancestors.");
        }
    }

    private static void ClearDirectoryContents(string outputDirectory)
    {
        foreach (var file in Directory.EnumerateFiles(outputDirectory))
        {
            File.Delete(file);
        }

        foreach (var directory in Directory.EnumerateDirectories(outputDirectory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static bool IsSameOrAncestor(string candidateAncestor, string path)
    {
        if (string.Equals(candidateAncestor, path, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return path.StartsWith(candidateAncestor + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(candidateAncestor + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDirectoryPath(string path)
        => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}
