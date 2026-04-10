using InSpectra.Gen.Tests.TestSupport;
using InSpectra.Gen.Targets;

namespace InSpectra.Gen.Tests.Commands;

public sealed class LocalCliTargetFactoryTests
{
    [Fact]
    public void Create_Escapes_Percent_Signs_In_Windows_Wrappers()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var temp = new TempDirectory();
        var sourcePath = Path.Combine(temp.Path, "demo.exe");
        File.WriteAllText(sourcePath, string.Empty);

        var factory = new LocalCliTargetFactory(new LocalCliFrameworkDetector());
        var target = factory.Create(
            sourcePath,
            ["%TEMP%", "literal"],
            temp.Path,
            Path.Combine(temp.Path, "shim"),
            commandName: null,
            cliFramework: null);

        var wrapper = File.ReadAllText(target.CommandPath);

        Assert.Contains("\"%%TEMP%%\"", wrapper, StringComparison.Ordinal);
        Assert.DoesNotContain("\"%TEMP%\"", wrapper, StringComparison.Ordinal);
    }
}
