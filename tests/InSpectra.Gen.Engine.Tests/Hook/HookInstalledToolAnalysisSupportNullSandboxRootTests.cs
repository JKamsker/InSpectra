namespace InSpectra.Gen.Engine.Tests.Hook;

using InSpectra.Gen.Engine.Modes.Hook.Capture;
using InSpectra.Gen.Engine.Modes.Hook.Execution;
using InSpectra.Gen.Engine.Tooling.Process;
using InSpectra.Gen.Engine.Tests.TestSupport;
using System.Text.Json;

public sealed class HookInstalledToolAnalysisSupportNullSandboxRootTests
{
    [Fact]
    public async Task AnalyzeInstalledAsync_Leaves_Sandbox_Root_Null_When_Not_Provided()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var hookDllPath = HookInstalledToolAnalysisTestSupport.CreateHookPlaceholder(tempDirectory.Path);
        var installedTool = HookInstalledToolAnalysisTestSupport.CreateInstalledToolWithCleanupRoot(
            tempDirectory,
            preferredEntryPointPath: null,
            cleanupRoot: null);
        var runtime = new HookInstalledToolAnalysisTestSupport.FakeHookCommandRuntime(invocation =>
        {
            Assert.Null(invocation.SandboxRoot);
            Assert.Equal(tempDirectory.Path, invocation.WorkingDirectory);

            var capturePath = invocation.Environment["INSPECTRA_CAPTURE_PATH"];
            File.WriteAllText(capturePath, JsonSerializer.Serialize(new HookCaptureResult
            {
                CaptureVersion = 1,
                Status = "ok",
                Root = HookInstalledToolAnalysisTestSupport.CreateValidRootCommand(),
            }));

            return new CommandRuntime.ProcessResult(
                Status: "ok",
                TimedOut: false,
                ExitCode: 0,
                DurationMs: 1,
                Stdout: string.Empty,
                Stderr: string.Empty);
        });
        var support = new HookInstalledToolAnalysisSupport(runtime, () => hookDllPath);
        var result = HookInstalledToolAnalysisTestSupport.CreateInitialResult();

        await support.AnalyzeInstalledAsync(
            new InstalledToolAnalysisRequest(result, "1.2.3", "demo", tempDirectory.Path, installedTool, tempDirectory.Path, 30),
            CancellationToken.None);

        Assert.Equal("success", result["disposition"]?.GetValue<string>());
    }
}
