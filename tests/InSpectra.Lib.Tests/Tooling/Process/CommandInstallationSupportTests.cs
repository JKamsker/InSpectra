namespace InSpectra.Lib.Tests.Tooling.Process;

using System.Text.Json.Nodes;
using InSpectra.Lib.Tooling.Process;
using InSpectra.Lib.Tests.TestSupport;

[Collection(DotnetHostEnvironmentCollection.Name)]
public sealed class CommandInstallationSupportTests
{
    [Fact]
    public async Task InstallToolAsync_Returns_CleanupRoot_From_Install_Sandbox()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var tempRoot = CreateSandboxRoot(tempDirectory.Path);
        var runtime = new RecordingInstallRuntime();
        var result = CreateResult();

        var installedTool = await CommandInstallationSupport.InstallToolAsync(
            runtime,
            result,
            packageId: "Demo.Tool",
            version: "1.2.3",
            commandName: "demo",
            tempRoot: tempRoot,
            installTimeoutSeconds: 30,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(installedTool);
        Assert.Equal(Path.GetFullPath(tempRoot), installedTool.CleanupRoot);
        Assert.Equal(Path.GetFullPath(tempRoot), runtime.LastSandboxRoot);
    }

    [Fact]
    public async Task InstallToolAsync_Uses_Resolved_Dotnet_Host_Path()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var tempRoot = CreateSandboxRoot(tempDirectory.Path);
        var dotnetRoot = Path.Combine(tempDirectory.Path, "fake-dotnet");
        InstalledDotnetToolCommandSupportTestSupport.WriteDotnetHost(dotnetRoot);
        var expectedHostPath = InstalledDotnetToolCommandSupportTestSupport.GetDotnetHostPath(dotnetRoot);
        using var dotnetEnvironment = new DotnetEnvironmentScope(dotnetHostPath: expectedHostPath);
        var runtime = new RecordingInstallRuntime();

        _ = await CommandInstallationSupport.InstallToolAsync(
            runtime,
            CreateResult(),
            packageId: "Demo.Tool",
            version: "1.2.3",
            commandName: "demo",
            tempRoot: tempRoot,
            installTimeoutSeconds: 30,
            cancellationToken: CancellationToken.None);

        Assert.Equal(Path.GetFullPath(expectedHostPath), runtime.LastFilePath);
    }

    [Fact]
    public async Task InstallToolAsync_Failure_Message_Preserves_Normalized_Stdout_And_Stderr()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var runtime = new RecordingInstallRuntime(new CommandRuntime.ProcessResult(
            Status: "failed",
            TimedOut: false,
            ExitCode: 1,
            DurationMs: 5,
            Stdout: "\u001b[32mstdout line\u001b[0m",
            Stderr: " stderr line \n"));
        var result = CreateResult();

        var installedTool = await CommandInstallationSupport.InstallToolAsync(
            runtime,
            result,
            packageId: "Demo.Tool",
            version: "1.2.3",
            commandName: "demo",
            tempRoot: CreateSandboxRoot(tempDirectory.Path),
            installTimeoutSeconds: 30,
            cancellationToken: CancellationToken.None);

        Assert.Null(installedTool);
        var failureMessage = result["failureMessage"]?.GetValue<string>();
        Assert.Equal("Tool installation failed. stdout: stdout line stderr: stderr line", failureMessage);
    }

    private static JsonObject CreateResult()
        => new()
        {
            ["steps"] = new JsonObject(),
            ["timings"] = new JsonObject(),
        };

    private static string CreateSandboxRoot(string tempDirectoryPath)
    {
        var tempRoot = Path.Combine(tempDirectoryPath, "sandbox");
        Directory.CreateDirectory(tempRoot);
        return tempRoot;
    }

    private static void WriteInstalledCommand(string installDirectory)
    {
        File.WriteAllText(Path.Combine(installDirectory, "demo"), string.Empty);
        File.WriteAllText(Path.Combine(installDirectory, "demo.cmd"), "@echo off");
    }

    private sealed class RecordingInstallRuntime : CommandRuntime
    {
        private readonly CommandRuntime.ProcessResult _result;

        public RecordingInstallRuntime(CommandRuntime.ProcessResult? result = null)
        {
            _result = result ?? new CommandRuntime.ProcessResult(
                Status: "ok",
                TimedOut: false,
                ExitCode: 0,
                DurationMs: 1,
                Stdout: string.Empty,
                Stderr: string.Empty);
        }

        public string? LastSandboxRoot { get; private set; }

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
            LastSandboxRoot = sandboxRoot;
            if (_result.ExitCode == 0 && !_result.TimedOut)
            {
                var installDirectory = Path.Combine(workingDirectory, "tool");
                Directory.CreateDirectory(installDirectory);
                WriteInstalledCommand(installDirectory);
            }

            return Task.FromResult(_result);
        }
    }
}
