using InSpectra.Gen.Runtime.Acquisition;

namespace InSpectra.Gen.Tests.Commands;

public class DotnetProjectArgsBuilderTests
{
    [Fact]
    public void Minimal_arguments_emits_run_project_and_terminator()
    {
        var args = DotnetProjectArgsBuilder.Build(
            new DotnetBuildSettings("/tmp/MyCli.csproj", null, null, null, false, false));

        Assert.Equal(new[] { "run", "--project", "/tmp/MyCli.csproj", "--" }, args);
    }

    [Fact]
    public void Configuration_is_passed_with_short_flag()
    {
        var args = DotnetProjectArgsBuilder.Build(
            new DotnetBuildSettings("proj.csproj", "Release", null, null, false, false));

        Assert.Equal(new[] { "run", "--project", "proj.csproj", "-c", "Release", "--" }, args);
    }

    [Fact]
    public void Framework_is_passed_with_short_flag()
    {
        var args = DotnetProjectArgsBuilder.Build(
            new DotnetBuildSettings("proj.csproj", null, "net10.0", null, false, false));

        Assert.Equal(new[] { "run", "--project", "proj.csproj", "-f", "net10.0", "--" }, args);
    }

    [Fact]
    public void Launch_profile_is_passed_with_long_flag()
    {
        var args = DotnetProjectArgsBuilder.Build(
            new DotnetBuildSettings("proj.csproj", null, null, "dev", false, false));

        Assert.Equal(new[] { "run", "--project", "proj.csproj", "--launch-profile", "dev", "--" }, args);
    }

    [Fact]
    public void No_build_and_no_restore_flags_are_appended_before_terminator()
    {
        var args = DotnetProjectArgsBuilder.Build(
            new DotnetBuildSettings("proj.csproj", null, null, null, true, true));

        Assert.Equal(new[] { "run", "--project", "proj.csproj", "--no-build", "--no-restore", "--" }, args);
    }

    [Fact]
    public void All_options_together_produce_expected_order()
    {
        var args = DotnetProjectArgsBuilder.Build(
            new DotnetBuildSettings("proj.csproj", "Release", "net10.0", "dev", true, true));

        Assert.Equal(
            new[]
            {
                "run",
                "--project",
                "proj.csproj",
                "-c",
                "Release",
                "-f",
                "net10.0",
                "--launch-profile",
                "dev",
                "--no-build",
                "--no-restore",
                "--",
            },
            args);
    }

    [Fact]
    public void Terminator_is_always_last_element()
    {
        var args = DotnetProjectArgsBuilder.Build(
            new DotnetBuildSettings("proj.csproj", "Debug", "net10.0", null, false, false));

        Assert.Equal("--", args[^1]);
    }

    [Fact]
    public void Whitespace_configuration_is_ignored()
    {
        var args = DotnetProjectArgsBuilder.Build(
            new DotnetBuildSettings("proj.csproj", "   ", null, null, false, false));

        Assert.DoesNotContain("-c", args);
    }
}
