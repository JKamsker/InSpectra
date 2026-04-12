namespace InSpectra.Gen.Engine.Tests.Orchestration;

using InSpectra.Gen.Engine.Contracts.Providers;
using InSpectra.Gen.Engine.Orchestration;
using InSpectra.Gen.Engine.Tooling.Process;
using InSpectra.Gen.Engine.Tests.TestSupport;

public sealed class AcquisitionAnalysisDispatcherTests
{
    [Fact]
    public void CreateInstalledToolContext_Uses_Explicit_Cleanup_Root()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var sandboxEnvironment = new CommandRuntime().CreateSandboxEnvironment(tempDirectory.Path);
        var installDirectory = Path.Combine(tempDirectory.Path, "tool");
        Directory.CreateDirectory(installDirectory);
        var target = CreateTarget(sandboxEnvironment.Values, installDirectory);

        var installedTool = AcquisitionAnalysisDispatcher.CreateInstalledToolContext(
            target,
            Path.GetFullPath(tempDirectory.Path));

        Assert.Equal(Path.GetFullPath(tempDirectory.Path), installedTool.CleanupRoot);
    }

    [Fact]
    public void CreateInstalledToolContext_Does_Not_Infer_Cleanup_Root_From_Public_Target_Data()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var sandboxEnvironment = new CommandRuntime().CreateSandboxEnvironment(tempDirectory.Path);
        var installDirectory = Path.Combine(tempDirectory.Path, "tool");
        Directory.CreateDirectory(installDirectory);
        var target = CreateTarget(sandboxEnvironment.Values, installDirectory);

        var installedTool = AcquisitionAnalysisDispatcher.CreateInstalledToolContext(target, cleanupRoot: null);

        Assert.Null(installedTool.CleanupRoot);
    }

    private static CliTargetDescriptor CreateTarget(
        IReadOnlyDictionary<string, string> environment,
        string installDirectory)
        => new(
            DisplayName: "Demo.Tool@1.2.3",
            CommandPath: Path.Combine(installDirectory, "demo.cmd"),
            CommandName: "demo",
            WorkingDirectory: installDirectory,
            InstallDirectory: installDirectory,
            PreferredEntryPointPath: null,
            Version: "1.2.3",
            Environment: environment,
            CliFramework: "System.CommandLine",
            HookCliFramework: "System.CommandLine");
}
