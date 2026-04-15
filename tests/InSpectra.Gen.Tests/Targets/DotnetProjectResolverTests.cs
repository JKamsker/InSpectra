using InSpectra.Lib;
using InSpectra.Gen.Commands.Generate;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.Targets;

public class DotnetProjectResolverTests
{
    [Fact]
    public void Explicit_csproj_path_returns_absolute_path()
    {
        using var temp = new TempDirectory();
        var projectPath = Path.Combine(temp.Path, "MyCli.csproj");
        File.WriteAllText(projectPath, "<Project />");

        var result = DotnetProjectResolver.Resolve("MyCli.csproj", temp.Path);

        Assert.Equal(projectPath, result);
    }

    [Fact]
    public void Absolute_csproj_path_is_returned_as_is()
    {
        using var temp = new TempDirectory();
        var projectPath = Path.Combine(temp.Path, "MyCli.csproj");
        File.WriteAllText(projectPath, "<Project />");

        var result = DotnetProjectResolver.Resolve(projectPath, Directory.GetCurrentDirectory());

        Assert.Equal(projectPath, result);
    }

    [Fact]
    public void Fsproj_path_is_accepted()
    {
        using var temp = new TempDirectory();
        var projectPath = Path.Combine(temp.Path, "MyCli.fsproj");
        File.WriteAllText(projectPath, "<Project />");

        var result = DotnetProjectResolver.Resolve("MyCli.fsproj", temp.Path);

        Assert.Equal(projectPath, result);
    }

    [Fact]
    public void Directory_with_single_project_returns_that_project()
    {
        using var temp = new TempDirectory();
        var projectPath = Path.Combine(temp.Path, "MyCli.csproj");
        File.WriteAllText(projectPath, "<Project />");

        var result = DotnetProjectResolver.Resolve(temp.Path, Directory.GetCurrentDirectory());

        Assert.Equal(projectPath, result);
    }

    [Fact]
    public void Directory_with_no_project_throws_usage_exception()
    {
        using var temp = new TempDirectory();

        var exception = Assert.Throws<CliUsageException>(
            () => DotnetProjectResolver.Resolve(temp.Path, Directory.GetCurrentDirectory()));

        Assert.Contains("No .NET project file", exception.Message);
    }

    [Fact]
    public void Directory_with_multiple_projects_throws_and_lists_names()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "First.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(temp.Path, "Second.csproj"), "<Project />");

        var exception = Assert.Throws<CliUsageException>(
            () => DotnetProjectResolver.Resolve(temp.Path, Directory.GetCurrentDirectory()));

        Assert.Contains("Multiple .NET project files", exception.Message);
        Assert.Contains("First.csproj", exception.Message);
        Assert.Contains("Second.csproj", exception.Message);
    }

    [Fact]
    public void Non_existent_path_throws_usage_exception()
    {
        using var temp = new TempDirectory();
        var missing = Path.Combine(temp.Path, "does-not-exist.csproj");

        var exception = Assert.Throws<CliUsageException>(
            () => DotnetProjectResolver.Resolve(missing, Directory.GetCurrentDirectory()));

        Assert.Contains("was not found", exception.Message);
    }

    [Fact]
    public void File_that_is_not_a_project_throws_usage_exception()
    {
        using var temp = new TempDirectory();
        var txtPath = Path.Combine(temp.Path, "readme.txt");
        File.WriteAllText(txtPath, "hello");

        var exception = Assert.Throws<CliUsageException>(
            () => DotnetProjectResolver.Resolve(txtPath, Directory.GetCurrentDirectory()));

        Assert.Contains("is not a .NET project file", exception.Message);
    }

    [Fact]
    public void Empty_value_throws_usage_exception()
    {
        var exception = Assert.Throws<CliUsageException>(
            () => DotnetProjectResolver.Resolve("  ", Directory.GetCurrentDirectory()));

        Assert.Contains("required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
