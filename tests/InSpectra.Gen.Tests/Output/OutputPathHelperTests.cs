using InSpectra.Lib;
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
    public async Task PublishFileAsync_Preserves_Existing_Output_When_Cancelled()
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "opencli.json");
        await File.WriteAllTextAsync(outputPath, "existing");
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            OutputPathHelper.PublishFileAsync(outputPath, "replacement", overwrite: true, cancellationSource.Token));

        Assert.Equal("existing", await File.ReadAllTextAsync(outputPath));
    }

    [Fact]
    public async Task PublishDirectoryAsync_Keeps_Existing_Output_When_Writer_Fails()
    {
        using var temp = new TempDirectory();
        var outputDirectory = Path.Combine(temp.Path, "out");
        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "keep.txt"), "existing");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            OutputPathHelper.PublishDirectoryAsync<bool>(
                outputDirectory,
                overwrite: true,
                async (stagingDirectory, cancellationToken) =>
                {
                    await File.WriteAllTextAsync(Path.Combine(stagingDirectory, "new.txt"), "new", cancellationToken);
                    throw new InvalidOperationException("boom");
#pragma warning disable CS0162
                    return true;
#pragma warning restore CS0162
                },
                CancellationToken.None));

        Assert.Equal("existing", await File.ReadAllTextAsync(Path.Combine(outputDirectory, "keep.txt")));
        Assert.False(File.Exists(Path.Combine(outputDirectory, "new.txt")));
    }

    [Fact]
    public async Task PublishDirectoryAsync_Replaces_Existing_Empty_Output_Directory()
    {
        using var temp = new TempDirectory();
        var outputDirectory = Path.Combine(temp.Path, "out");
        Directory.CreateDirectory(outputDirectory);

        await OutputPathHelper.PublishDirectoryAsync<bool>(
            outputDirectory,
            overwrite: false,
            async (stagingDirectory, cancellationToken) =>
            {
                await File.WriteAllTextAsync(Path.Combine(stagingDirectory, "new.txt"), "new", cancellationToken);
                return true;
            },
            CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(outputDirectory, "new.txt")));
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
