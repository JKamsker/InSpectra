namespace InSpectra.Lib.Tests.Hook;

using InSpectra.Lib.Modes.Hook.Capture;
using InSpectra.Lib.Modes.Hook.Execution;
using InSpectra.Lib.Tooling.Process;
using InSpectra.Lib.Tests.TestSupport;

using System.Text.Json;

public sealed class HookInstalledToolAnalysisSupportRetryTests
{
    [Fact]
    public async Task AnalyzeInstalledAsync_Preserves_Sandbox_Root_During_Alternate_Help_Retry()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var hookDllPath = HookInstalledToolAnalysisTestSupport.CreateHookPlaceholder(tempDirectory.Path);
        var installedTool = HookInstalledToolAnalysisTestSupport.CreateInstalledTool(tempDirectory);
        var expectedSandboxRoot = Path.GetFullPath(tempDirectory.Path);
        var invocations = new List<HookInstalledToolAnalysisTestSupport.HookInvocation>();
        var runtime = new HookInstalledToolAnalysisTestSupport.FakeHookCommandRuntime(invocation =>
        {
            invocations.Add(invocation);
            Assert.Equal(expectedSandboxRoot, invocation.SandboxRoot);

            if (invocations.Count == 1)
            {
                return new CommandRuntime.ProcessResult(
                    Status: "failed",
                    TimedOut: false,
                    ExitCode: 1,
                    DurationMs: 8,
                    Stdout: string.Empty,
                    Stderr: "error: Unrecognized command or argument '--help'.");
            }

            var capturePath = invocation.Environment["INSPECTRA_CAPTURE_PATH"];
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
            new InstalledToolAnalysisRequest(result, "1.2.3", "demo", tempDirectory.Path, installedTool, tempDirectory.Path, 30),
            CancellationToken.None);

        Assert.Equal("success", result["disposition"]?.GetValue<string>());
        Assert.True(invocations.Count >= 2);
        Assert.Contains(invocations, invocation => invocation.ArgumentList.SequenceEqual(["--help"]));
        Assert.Contains(invocations, invocation => !invocation.ArgumentList.SequenceEqual(["--help"]));
    }
}
