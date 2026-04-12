using InSpectra.Gen.Core;

namespace InSpectra.Gen.Engine.Rendering.Pipeline;

internal static class OutputPathHelper
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
            throw new CliUsageException($"Output file `{outputFile}` already exists. Enable overwrite to replace it.");
        }
    }

    public static async Task PublishFileAsync(
        string outputFile,
        string content,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        var publication = PrepareFilePublication(outputFile, overwrite);
        try
        {
            await File.WriteAllTextAsync(publication.TempPath, content, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            publication.Commit();
        }
        finally
        {
            publication.Dispose();
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
            throw new CliUsageException($"Output directory `{outputDirectory}` is not empty. Enable overwrite to replace it.");
        }

        EnsureDirectoryCanBeReplaced(resolvedOutputDirectory);
        ClearDirectoryContents(resolvedOutputDirectory);
    }

    public static async Task<TResult> PublishDirectoryAsync<TResult>(
        string outputDirectory,
        bool overwrite,
        Func<string, CancellationToken, Task<TResult>> writeAsync,
        CancellationToken cancellationToken)
    {
        var publication = PrepareDirectoryPublication(outputDirectory, overwrite);
        try
        {
            var result = await writeAsync(publication.StagingPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            publication.Commit();
            return result;
        }
        finally
        {
            publication.Dispose();
        }
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

    private static OutputPathFilePublication PrepareFilePublication(string outputFile, bool overwrite)
    {
        var resolvedOutputFile = Path.GetFullPath(outputFile);
        EnsureFileWritable(resolvedOutputFile, overwrite);

        var directory = Path.GetDirectoryName(resolvedOutputFile);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new OutputPathFilePublication(
            resolvedOutputFile,
            resolvedOutputFile + $".{Guid.NewGuid():N}.tmp",
            overwrite);
    }

    private static OutputPathDirectoryPublication PrepareDirectoryPublication(string outputDirectory, bool overwrite)
    {
        var resolvedOutputDirectory = NormalizeDirectoryPath(outputDirectory);
        if (File.Exists(resolvedOutputDirectory))
        {
            throw new CliUsageException($"Output directory `{outputDirectory}` points to an existing file.");
        }

        var targetExists = Directory.Exists(resolvedOutputDirectory);
        if (targetExists)
        {
            var hasEntries = Directory.EnumerateFileSystemEntries(resolvedOutputDirectory).Any();
            if (hasEntries && !overwrite)
            {
                throw new CliUsageException($"Output directory `{outputDirectory}` is not empty. Enable overwrite to replace it.");
            }

            if (hasEntries)
            {
                EnsureDirectoryCanBeReplaced(resolvedOutputDirectory);
            }
        }

        var parentDirectory = Path.GetDirectoryName(resolvedOutputDirectory);
        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }

        var stagingPath = resolvedOutputDirectory + $".{Guid.NewGuid():N}.tmp";
        Directory.CreateDirectory(stagingPath);
        return new OutputPathDirectoryPublication(resolvedOutputDirectory, stagingPath, overwrite, targetExists);
    }
}
