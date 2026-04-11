namespace InSpectra.Gen.Acquisition.Tests.Hook;

using InSpectra.Gen.Acquisition.Modes.Hook.Capture;
using InSpectra.Gen.Acquisition.Modes.Hook.Execution;
using InSpectra.Gen.Acquisition.Tooling.Process;
using InSpectra.Gen.Acquisition.Tests.TestSupport;

using System.Text.Json;

public sealed class HookInstalledToolAnalysisSupportPathTests
{
    [Fact]
    public async Task AnalyzeInstalledAsync_Keeps_Capture_Output_Separate_From_Working_Directory()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var hookDllPath = HookInstalledToolAnalysisTestSupport.CreateHookPlaceholder(tempDirectory.Path);
        var outputDirectory = Path.Combine(tempDirectory.Path, "artifacts");
        var workingDirectory = Path.Combine(tempDirectory.Path, "workspace");
        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(workingDirectory);

        var installedTool = HookInstalledToolAnalysisTestSupport.CreateInstalledTool(tempDirectory);
        var runtime = new HookInstalledToolAnalysisTestSupport.FakeHookCommandRuntime(invocation =>
        {
            Assert.Equal(workingDirectory, invocation.WorkingDirectory);
            Assert.Equal(workingDirectory, invocation.SandboxRoot);

            var capturePath = invocation.Environment["INSPECTRA_CAPTURE_PATH"];
            Assert.StartsWith(outputDirectory, capturePath, StringComparison.OrdinalIgnoreCase);
            Assert.False(capturePath.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase));

            File.WriteAllText(capturePath, JsonSerializer.Serialize(new HookCaptureResult
            {
                CaptureVersion = 1,
                Status = "ok",
                CliFramework = "System.CommandLine",
                Root = HookInstalledToolAnalysisTestSupport.CreateValidRootCommand(),
            }));

            return new CommandRuntime.ProcessResult(
                Status: "ok",
                TimedOut: false,
                ExitCode: 0,
                DurationMs: 8,
                Stdout: string.Empty,
                Stderr: string.Empty);
        });
        var support = new HookInstalledToolAnalysisSupport(runtime, () => hookDllPath);
        var result = HookInstalledToolAnalysisTestSupport.CreateInitialResult();

        await support.AnalyzeInstalledAsync(
            new InstalledToolAnalysisRequest(result, "1.2.3", "demo", outputDirectory, installedTool, workingDirectory, 30),
            CancellationToken.None);

        Assert.Equal("success", result["disposition"]?.GetValue<string>());
        Assert.Equal("opencli.json", result["artifacts"]?["opencliArtifact"]?.GetValue<string>());
    }
}
