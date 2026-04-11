using InSpectra.Gen.Acquisition.Contracts.Exceptions;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.Output;

public sealed class OutputPathHelperTests
{
    [Fact]
    public void PrepareDirectory_Rejects_Current_Working_Directory_Overwrite()
    {
        using var temp = new TempDirectory();
        var existingFile = Path.Combine(temp.Path, "keep.txt");
        File.WriteAllText(existingFile, "existing");

        var originalDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = temp.Path;

            var exception = Assert.Throws<CliUsageException>(() => OutputPathHelper.PrepareDirectory(".", overwrite: true));

            Assert.Contains("current working directory", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(existingFile));
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
        }
    }

    [Fact]
    public void PrepareDirectory_Clears_Existing_Output_Contents_When_Target_Is_Safe()
    {
        using var temp = new TempDirectory();
        var outputDirectory = Path.Combine(temp.Path, "out");
        Directory.CreateDirectory(Path.Combine(outputDirectory, "nested"));
        File.WriteAllText(Path.Combine(outputDirectory, "old.txt"), "old");
        File.WriteAllText(Path.Combine(outputDirectory, "nested", "old.txt"), "old");

        OutputPathHelper.PrepareDirectory(outputDirectory, overwrite: true);

        Assert.True(Directory.Exists(outputDirectory));
        Assert.Empty(Directory.EnumerateFileSystemEntries(outputDirectory));
    }

    [Fact]
    public void EnsureFileWritable_Rejects_Existing_Directory_Path()
    {
        using var temp = new TempDirectory();

        var exception = Assert.Throws<CliUsageException>(() => OutputPathHelper.EnsureFileWritable(temp.Path, overwrite: true));

        Assert.Contains("existing directory", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrepareDirectory_Rejects_Existing_File_Path()
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "out");
        File.WriteAllText(outputPath, "file");

        var exception = Assert.Throws<CliUsageException>(() => OutputPathHelper.PrepareDirectory(outputPath, overwrite: true));

        Assert.Contains("existing file", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
