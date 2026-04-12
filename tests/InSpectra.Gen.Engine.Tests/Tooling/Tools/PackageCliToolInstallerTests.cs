namespace InSpectra.Gen.Engine.Tests.Tooling.Tools;

using InSpectra.Gen.Engine.Tooling.Packages;
using InSpectra.Gen.Engine.Tooling.Process;
using InSpectra.Gen.Engine.Tooling.Tools;
using InSpectra.Gen.Engine.Tests.Tooling.Process;
using InSpectra.Gen.Engine.Tests.TestSupport;

[Collection(DotnetHostEnvironmentCollection.Name)]
public sealed class PackageCliToolInstallerTests
{
    [Fact]
    public async Task InstallAsync_Uses_Resolved_Dotnet_Host_Path()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var tempRoot = Path.Combine(tempDirectory.Path, "sandbox");
        Directory.CreateDirectory(tempRoot);
        var dotnetRoot = Path.Combine(tempDirectory.Path, "fake-dotnet");
        InstalledDotnetToolCommandSupportTestSupport.WriteDotnetHost(dotnetRoot);
        var expectedHostPath = InstalledDotnetToolCommandSupportTestSupport.GetDotnetHostPath(dotnetRoot);
        using var dotnetEnvironment = new DotnetEnvironmentScope(dotnetHostPath: expectedHostPath);
        var runtime = new RecordingInstallRuntime();
        var installer = new PackageCliToolInstaller(runtime, new StubToolDescriptorResolver());

        var installation = await installer.InstallAsync(
            packageId: "Demo.Tool",
            version: "1.2.3",
            commandName: null,
            cliFramework: null,
            tempRoot: tempRoot,
            timeoutSeconds: 30,
            cancellationToken: CancellationToken.None);

        Assert.Equal("demo", installation.CommandName);
        Assert.Equal(Path.GetFullPath(expectedHostPath), runtime.LastFilePath);
    }

    private static void WriteInstalledCommand(string installDirectory)
    {
        File.WriteAllText(Path.Combine(installDirectory, "demo"), string.Empty);
        File.WriteAllText(Path.Combine(installDirectory, "demo.cmd"), "@echo off");
    }

    private sealed class StubToolDescriptorResolver : IToolDescriptorResolver
    {
        public Task<ToolDescriptorResolution> ResolveAsync(
            string packageId,
            string version,
            string? commandName,
            CancellationToken cancellationToken)
        {
            var descriptor = new ToolDescriptor(
                PackageId: packageId,
                Version: version,
                CommandName: commandName ?? "demo",
                CliFramework: "clifx",
                PreferredAnalysisMode: "help",
                SelectionReason: "test",
                PackageUrl: "https://example.test/package",
                PackageContentUrl: null,
                CatalogEntryUrl: null);
            return Task.FromResult(new ToolDescriptorResolution(descriptor, SpectrePackageInspection.Empty));
        }
    }

    private sealed class RecordingInstallRuntime : CommandRuntime
    {
        public string? LastFilePath { get; private set; }

        public override Task<CommandRuntime.ProcessResult> InvokeProcessCaptureAsync(
            string filePath,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environment,
            int timeoutSeconds,
            string? sandboxRoot,
            CancellationToken cancellationToken)
        {
            LastFilePath = filePath;
            var installDirectory = Path.Combine(workingDirectory, "tool");
            Directory.CreateDirectory(installDirectory);
            WriteInstalledCommand(installDirectory);
            return Task.FromResult(new CommandRuntime.ProcessResult(
                Status: "ok",
                TimedOut: false,
                ExitCode: 0,
                DurationMs: 1,
                Stdout: string.Empty,
                Stderr: string.Empty));
        }
    }
}
