namespace InSpectra.Lib.Tests.Hook;

using InSpectra.Lib.Modes.Hook.Capture;
using InSpectra.Lib.Modes.Hook.Execution;
using InSpectra.Lib.Tooling.Process;
using InSpectra.Lib.Tests.TestSupport;

using System.Text.Json;

public sealed class HookInstalledToolAnalysisSupportStatusTests
{
    [Theory]
    [InlineData("target-unhandled-exception")]
    [InlineData("capture-failed")]
    [InlineData("no-patchable-method")]
    public async Task AnalyzeInstalledAsync_Reports_NonOk_Hook_Capture_Statuses(string captureStatus)
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var hookDllPath = HookInstalledToolAnalysisTestSupport.CreateHookPlaceholder(tempDirectory.Path);
        var installedTool = HookInstalledToolAnalysisTestSupport.CreateInstalledTool(tempDirectory);
        var runtime = new HookInstalledToolAnalysisTestSupport.FakeHookCommandRuntime(invocation =>
        {
            var capturePath = invocation.Environment["INSPECTRA_CAPTURE_PATH"];
            File.WriteAllText(capturePath, JsonSerializer.Serialize(new HookCaptureResult
            {
                CaptureVersion = 1,
                Status = captureStatus,
                Error = $"{captureStatus} details",
            }));

            return new CommandRuntime.ProcessResult(
                Status: "failed",
                TimedOut: false,
                ExitCode: 1,
                DurationMs: 12,
                Stdout: string.Empty,
                Stderr: string.Empty);
        });
        var support = new HookInstalledToolAnalysisSupport(runtime, () => hookDllPath);
        var result = HookInstalledToolAnalysisTestSupport.CreateInitialResult();

        await support.AnalyzeInstalledAsync(
            new InstalledToolAnalysisRequest(result, "1.2.3", "demo", tempDirectory.Path, installedTool, tempDirectory.Path, 30),
            CancellationToken.None);

        Assert.Equal("retryable-failure", result["disposition"]?.GetValue<string>());
        Assert.Equal("hook-capture", result["phase"]?.GetValue<string>());
        Assert.Equal($"hook-{captureStatus}", result["classification"]?.GetValue<string>());
        Assert.Equal($"{captureStatus} details", result["failureMessage"]?.GetValue<string>());
    }

    [Fact]
    public void Hook_capture_version_mismatch_is_reported_explicitly()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var capturePath = Path.Combine(tempDirectory.Path, "inspectra-capture.json");
        File.WriteAllText(capturePath, JsonSerializer.Serialize(new HookCaptureResult
        {
            CaptureVersion = 99,
            Status = "ok",
        }));

        var capture = HookCaptureDeserializer.Deserialize(capturePath);

        Assert.NotNull(capture);
        Assert.Equal("capture-version-mismatch", capture!.Status);
        Assert.Contains("Expected `1`", capture.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnalyzeInstalledAsync_Treats_Capture_Version_Mismatch_As_Terminal()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var hookDllPath = HookInstalledToolAnalysisTestSupport.CreateHookPlaceholder(tempDirectory.Path);
        var installedTool = HookInstalledToolAnalysisTestSupport.CreateInstalledTool(tempDirectory);
        var runtime = new HookInstalledToolAnalysisTestSupport.FakeHookCommandRuntime(invocation =>
        {
            var capturePath = invocation.Environment["INSPECTRA_CAPTURE_PATH"];
            File.WriteAllText(capturePath, JsonSerializer.Serialize(new HookCaptureResult
            {
                CaptureVersion = 99,
                Status = "ok",
            }));

            return new CommandRuntime.ProcessResult(
                Status: "ok",
                TimedOut: false,
                ExitCode: 0,
                DurationMs: 15,
                Stdout: string.Empty,
                Stderr: string.Empty);
        });
        var support = new HookInstalledToolAnalysisSupport(runtime, () => hookDllPath);
        var result = HookInstalledToolAnalysisTestSupport.CreateInitialResult();

        await support.AnalyzeInstalledAsync(
            new InstalledToolAnalysisRequest(result, "1.2.3", "demo", tempDirectory.Path, installedTool, tempDirectory.Path, 30),
            CancellationToken.None);

        Assert.Equal("terminal-failure", result["disposition"]?.GetValue<string>());
        Assert.False(result["retryEligible"]?.GetValue<bool>() ?? true);
        Assert.Equal("hook-capture-version-mismatch", result["classification"]?.GetValue<string>());
    }
}
