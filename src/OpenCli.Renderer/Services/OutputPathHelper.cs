using OpenCli.Renderer.Runtime;

namespace OpenCli.Renderer.Services;

public static class OutputPathHelper
{
    public static void EnsureFileWritable(string outputFile, bool overwrite)
    {
        if (File.Exists(outputFile) && !overwrite)
        {
            throw new CliUsageException($"Output file `{outputFile}` already exists. Use `--overwrite` to replace it.");
        }
    }

    public static void PrepareDirectory(string outputDirectory, bool overwrite)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
            return;
        }

        if (!Directory.EnumerateFileSystemEntries(outputDirectory).Any())
        {
            return;
        }

        if (!overwrite)
        {
            throw new CliUsageException($"Output directory `{outputDirectory}` is not empty. Use `--overwrite` to replace it.");
        }

        Directory.Delete(outputDirectory, recursive: true);
        Directory.CreateDirectory(outputDirectory);
    }
}
