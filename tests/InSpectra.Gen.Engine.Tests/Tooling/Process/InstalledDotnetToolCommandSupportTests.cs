namespace InSpectra.Gen.Engine.Tests.Tooling.Process;

using InSpectra.Gen.Engine.Tests.TestSupport;
using InSpectra.Gen.Engine.Tooling.Process;
using static InSpectra.Gen.Engine.Tests.Tooling.Process.InstalledDotnetToolCommandSupportTestSupport;

[Collection("InstalledDotnetToolCommandSupport")]
public sealed class InstalledDotnetToolCommandSupportTests
{
    [Fact]
    public void TryResolveTargetFrameworkMonikerFromSettingsPath_Ignores_Parent_Directory_Named_Tools()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var settingsPath = Path.Combine(
            tempDirectory.Path,
            "outer",
            "tools",
            "workspace",
            "tool",
            "tools",
            "net8.0",
            "any",
            "DotnetToolSettings.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, "<DotNetCliTool />");

        var resolved = DotnetTargetFrameworkRuntimeSupport.TryResolveTargetFrameworkMonikerFromSettingsPath(
            settingsPath,
            out var targetFrameworkMoniker);

        Assert.True(resolved);
        Assert.Equal("net8.0", targetFrameworkMoniker);
    }

    [Fact]
    public void TryResolve_Prefers_Highest_Compatible_Target_Framework()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var installDirectory = Path.Combine(tempDirectory.Path, "tool");
        var dotnetRoot = Path.Combine(tempDirectory.Path, "dotnet");

        WriteDotnetHost(dotnetRoot);
        WriteSharedFramework(dotnetRoot, "Microsoft.NETCore.App", "8.0.7");
        WriteSharedFramework(dotnetRoot, "Microsoft.NETCore.App", "10.0.2");
        WriteCommandSettings(installDirectory, "net8.0", "demo-net8.dll");
        WriteCommandSettings(installDirectory, "net10.0", "demo-net10.dll");

        using var _ = new DotnetEnvironmentScope(dotnetRoot: dotnetRoot);

        var command = InstalledDotnetToolCommandSupport.TryResolve(installDirectory, "demo");

        Assert.NotNull(command);
        Assert.EndsWith(Path.Combine("tools", "net10.0", "any", "demo-net10.dll"), command!.EntryPointPath);
    }

    [Fact]
    public void TryResolve_Rejects_Higher_Incompatible_Target_Framework()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var installDirectory = Path.Combine(tempDirectory.Path, "tool");
        var dotnetRoot = Path.Combine(tempDirectory.Path, "dotnet");

        WriteDotnetHost(dotnetRoot);
        WriteSharedFramework(dotnetRoot, "Microsoft.NETCore.App", "8.0.7");
        WriteCommandSettings(installDirectory, "net8.0", "demo-net8.dll");
        WriteCommandSettings(installDirectory, "net10.0", "demo-net10.dll");

        using var _ = new DotnetEnvironmentScope(dotnetRoot: dotnetRoot);

        var command = InstalledDotnetToolCommandSupport.TryResolve(installDirectory, "demo");

        Assert.NotNull(command);
        Assert.EndsWith(Path.Combine("tools", "net8.0", "any", "demo-net8.dll"), command!.EntryPointPath);
    }

    [Fact]
    public void TryResolve_Without_Runtime_Inventory_Prefers_Lower_Known_Framework_Requirement()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var installDirectory = Path.Combine(tempDirectory.Path, "tool");
        var dotnetRoot = Path.Combine(tempDirectory.Path, "dotnet");

        WriteDotnetHost(dotnetRoot);
        Directory.CreateDirectory(dotnetRoot);
        WriteCommandSettings(installDirectory, "net8.0", "demo-net8.dll");
        WriteCommandSettings(installDirectory, "net10.0", "demo-net10.dll");

        using var _ = new DotnetEnvironmentScope(dotnetRoot: dotnetRoot);

        var command = InstalledDotnetToolCommandSupport.TryResolve(installDirectory, "demo");

        Assert.NotNull(command);
        Assert.EndsWith(Path.Combine("tools", "net8.0", "any", "demo-net8.dll"), command!.EntryPointPath);
    }

    [Fact]
    public void TryResolve_Treats_Windows_Target_Framework_As_Runnable_On_NetCore_Runtime()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var installDirectory = Path.Combine(tempDirectory.Path, "tool");
        var dotnetRoot = Path.Combine(tempDirectory.Path, "dotnet");

        WriteDotnetHost(dotnetRoot);
        WriteSharedFramework(dotnetRoot, "Microsoft.NETCore.App", "8.0.7");
        WriteCommandSettings(installDirectory, "net7.0", "demo-net7.dll");
        WriteCommandSettings(installDirectory, "net8.0-windows", "demo-net8-windows.dll");
        WriteRuntimeConfig(
            Path.Combine(installDirectory, "tools", "net8.0-windows", "any", "demo-net8-windows.dll"),
            "Microsoft.NETCore.App",
            "8.0.0");

        using var _ = new DotnetEnvironmentScope(dotnetRoot: dotnetRoot);

        var command = InstalledDotnetToolCommandSupport.TryResolve(installDirectory, "demo");

        Assert.NotNull(command);
        Assert.EndsWith(Path.Combine("tools", "net8.0-windows", "any", "demo-net8-windows.dll"), command!.EntryPointPath);
    }

    [Fact]
    public void TryResolve_Rejects_Windows_Target_Framework_When_RuntimeConfig_Requires_WindowsDesktop()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var installDirectory = Path.Combine(tempDirectory.Path, "tool");
        var dotnetRoot = Path.Combine(tempDirectory.Path, "dotnet");

        WriteDotnetHost(dotnetRoot);
        WriteSharedFramework(dotnetRoot, "Microsoft.NETCore.App", "8.0.7");
        WriteCommandSettings(installDirectory, "net7.0", "demo-net7.dll");
        WriteCommandSettings(installDirectory, "net8.0-windows", "demo-net8-windows.dll");
        WriteRuntimeConfig(
            Path.Combine(installDirectory, "tools", "net8.0-windows", "any", "demo-net8-windows.dll"),
            "Microsoft.WindowsDesktop.App",
            "8.0.0");

        using var _ = new DotnetEnvironmentScope(dotnetRoot: dotnetRoot);

        var command = InstalledDotnetToolCommandSupport.TryResolve(installDirectory, "demo");

        Assert.NotNull(command);
        Assert.EndsWith(Path.Combine("tools", "net7.0", "any", "demo-net7.dll"), command!.EntryPointPath);
    }

    [Fact]
    public void TryResolve_Rejects_MultiFramework_RuntimeConfig_When_A_Required_Framework_Is_Missing()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var installDirectory = Path.Combine(tempDirectory.Path, "tool");
        var dotnetRoot = Path.Combine(tempDirectory.Path, "dotnet");

        WriteDotnetHost(dotnetRoot);
        WriteSharedFramework(dotnetRoot, "Microsoft.NETCore.App", "8.0.7");
        WriteCommandSettings(installDirectory, "net7.0", "demo-net7.dll");
        WriteCommandSettings(installDirectory, "net8.0-windows", "demo-net8-windows.dll");
        WriteRuntimeConfigFrameworks(
            Path.Combine(installDirectory, "tools", "net8.0-windows", "any", "demo-net8-windows.dll"),
            ("Microsoft.NETCore.App", "8.0.0"),
            ("Microsoft.WindowsDesktop.App", "8.0.0"));

        using var _ = new DotnetEnvironmentScope(dotnetRoot: dotnetRoot);

        var command = InstalledDotnetToolCommandSupport.TryResolve(installDirectory, "demo");

        Assert.NotNull(command);
        Assert.EndsWith(Path.Combine("tools", "net7.0", "any", "demo-net7.dll"), command!.EntryPointPath);
    }

    [Fact]
    public void TryResolve_Uses_Deterministic_Fallback_When_Compatibility_Is_Unknown()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var installDirectory = Path.Combine(tempDirectory.Path, "tool");
        var dotnetRoot = Path.Combine(tempDirectory.Path, "dotnet");

        WriteDotnetHost(dotnetRoot);
        Directory.CreateDirectory(dotnetRoot);
        WriteCommandSettings(installDirectory, "custom-zeta", "demo-zeta.dll");
        WriteCommandSettings(installDirectory, "custom-alpha", "demo-alpha.dll");

        using var _ = new DotnetEnvironmentScope(dotnetRoot: dotnetRoot);

        var command = InstalledDotnetToolCommandSupport.TryResolve(installDirectory, "demo");

        Assert.NotNull(command);
        Assert.EndsWith(Path.Combine("tools", "custom-alpha", "any", "demo-alpha.dll"), command!.EntryPointPath);
    }

    [Fact]
    public void TryResolve_Uses_The_Same_Host_Inventory_That_Hook_Execution_Will_Launch()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var installDirectory = Path.Combine(tempDirectory.Path, "tool");
        var staleDotnetRoot = Path.Combine(tempDirectory.Path, "stale-dotnet");
        var actualHostRoot = Path.Combine(tempDirectory.Path, "actual-dotnet");

        WriteDotnetHost(staleDotnetRoot);
        WriteSharedFramework(staleDotnetRoot, "Microsoft.NETCore.App", "10.0.2");
        WriteDotnetHost(actualHostRoot);
        WriteSharedFramework(actualHostRoot, "Microsoft.NETCore.App", "8.0.7");
        WriteCommandSettings(installDirectory, "net8.0", "demo-net8.dll");
        WriteCommandSettings(installDirectory, "net10.0", "demo-net10.dll");

        using var _ = new DotnetEnvironmentScope(
            dotnetRoot: staleDotnetRoot,
            dotnetHostPath: GetDotnetHostPath(actualHostRoot));

        var command = InstalledDotnetToolCommandSupport.TryResolve(installDirectory, "demo");

        Assert.NotNull(command);
        Assert.EndsWith(Path.Combine("tools", "net8.0", "any", "demo-net8.dll"), command!.EntryPointPath);
    }

    [Fact]
    public void TryResolve_Uses_Ancestor_Dotnet_Root_For_Host_Paths_Under_Bin()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var installDirectory = Path.Combine(tempDirectory.Path, "tool");
        var dotnetRoot = Path.Combine(tempDirectory.Path, "actual-dotnet");
        var hostDirectory = Path.Combine(dotnetRoot, "bin");

        WriteDotnetHost(hostDirectory);
        WriteSharedFramework(dotnetRoot, "Microsoft.NETCore.App", "10.0.2");
        WriteCommandSettings(installDirectory, "net8.0", "demo-net8.dll");
        WriteCommandSettings(installDirectory, "net10.0", "demo-net10.dll");

        using var _ = new DotnetEnvironmentScope(dotnetHostPath: GetDotnetHostPath(hostDirectory));

        var command = InstalledDotnetToolCommandSupport.TryResolve(installDirectory, "demo");

        Assert.NotNull(command);
        Assert.EndsWith(Path.Combine("tools", "net10.0", "any", "demo-net10.dll"), command!.EntryPointPath);
    }
}

[CollectionDefinition("InstalledDotnetToolCommandSupport", DisableParallelization = true)]
public sealed class InstalledDotnetToolCommandSupportCollectionDefinition;
