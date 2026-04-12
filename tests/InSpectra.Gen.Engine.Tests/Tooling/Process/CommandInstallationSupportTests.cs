namespace InSpectra.Gen.Engine.Tests.Tooling.Process;

using System.Text.Json.Nodes;
using InSpectra.Gen.Engine.Tooling.Process;
using InSpectra.Gen.Engine.Tests.TestSupport;

public sealed class CommandInstallationSupportTests
{
    [Fact]
    public async Task InstallToolAsync_Returns_CleanupRoot_From_Install_Sandbox()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var tempRoot = Path.Combine(tempDirectory.Path, "sandbox");
        Directory.CreateDirectory(tempRoot);
        var runtime = new RecordingInstallRuntime();
        var result = new JsonObject
        {
            ["steps"] = new JsonObject(),
            ["timings"] = new JsonObject(),
        };

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

    private sealed class RecordingInstallRuntime : CommandRuntime
    {
        public string? LastSandboxRoot { get; private set; }

        public override Task<ProcessResult> InvokeProcessCaptureAsync(
            string filePath,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environment,
            int timeoutSeconds,
            string? sandboxRoot,
            CancellationToken cancellationToken)
        {
            LastSandboxRoot = sandboxRoot;
            var installDirectory = Path.Combine(workingDirectory, "tool");
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "demo.cmd"), "@echo off");
            return Task.FromResult(new ProcessResult(
                Status: "ok",
                TimedOut: false,
                ExitCode: 0,
                DurationMs: 1,
                Stdout: string.Empty,
                Stderr: string.Empty));
        }
    }
}
