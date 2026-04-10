using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.Commands;

public class ExecutableResolverTests
{
    [Fact]
    public void Resolve_accepts_bare_name_file_name_and_full_path()
    {
        using var temp = new TempDirectory();
        var executableFileName = OperatingSystem.IsWindows() ? "demo.cmd" : "demo";
        var executablePath = System.IO.Path.Combine(temp.Path, executableFileName);
        File.WriteAllText(executablePath, "@echo off");

        var resolver = new ExecutableResolver();

        Assert.Equal(executablePath, resolver.Resolve("demo", temp.Path));
        Assert.Equal(executablePath, resolver.Resolve(executableFileName, temp.Path));
        Assert.Equal(executablePath, resolver.Resolve(executablePath, temp.Path));
    }

    [Fact]
    public void Resolve_prefers_pathext_match_over_extensionless_file_on_windows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var temp = new TempDirectory();
        File.WriteAllText(System.IO.Path.Combine(temp.Path, "npm"), "shim");
        var commandPath = System.IO.Path.Combine(temp.Path, "npm.cmd");
        File.WriteAllText(commandPath, "@echo off");

        var resolver = new ExecutableResolver();

        Assert.Equal(commandPath, resolver.Resolve("npm", temp.Path));
    }
}
