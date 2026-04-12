namespace InSpectra.Gen.Engine.Tests.Tooling.FrameworkDetection;

using InSpectra.Gen.Engine.Tooling.FrameworkDetection;

public sealed class LocalCliFrameworkDetectorTests
{
    [Fact]
    public void Detect_Returns_CommandLineParser_When_CommandLine_Dll_Is_Present()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"inspectra-detector-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            // Arrange: place a dummy CommandLine.dll in the temp directory
            File.WriteAllBytes(Path.Combine(tempDir, "CommandLine.dll"), [0]);

            var detector = new LocalCliFrameworkDetector();

            // Act
            var detection = detector.Detect(tempDir);

            // Assert
            Assert.True(detection.HasManagedAssemblies, "Should detect managed assemblies in the directory.");
            Assert.NotNull(detection.CliFramework);
            Assert.Contains("CommandLineParser", detection.CliFramework, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(detection.HookCliFramework);
            Assert.Contains("CommandLineParser", detection.HookCliFramework, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Detect_Returns_Empty_When_Directory_Does_Not_Exist()
    {
        var detector = new LocalCliFrameworkDetector();
        var detection = detector.Detect(Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid():N}"));

        Assert.Null(detection.CliFramework);
        Assert.Null(detection.HookCliFramework);
        Assert.False(detection.HasManagedAssemblies);
    }

    [Fact]
    public void Detect_Returns_Empty_When_No_Assemblies_Are_Present()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"inspectra-detector-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "readme.txt"), "not an assembly");

            var detector = new LocalCliFrameworkDetector();
            var detection = detector.Detect(tempDir);

            Assert.Null(detection.CliFramework);
            Assert.Null(detection.HookCliFramework);
            Assert.False(detection.HasManagedAssemblies);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Detect_Returns_SystemCommandLine_When_System_CommandLine_Dll_Is_Present()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"inspectra-detector-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllBytes(Path.Combine(tempDir, "System.CommandLine.dll"), [0]);

            var detector = new LocalCliFrameworkDetector();
            var detection = detector.Detect(tempDir);

            Assert.True(detection.HasManagedAssemblies);
            Assert.NotNull(detection.CliFramework);
            Assert.Contains("System.CommandLine", detection.CliFramework, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(detection.HookCliFramework);
            Assert.Contains("System.CommandLine", detection.HookCliFramework, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
