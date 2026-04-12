namespace InSpectra.Gen.Engine.Tests.Hook;

using InSpectra.Gen.Engine.Modes.Hook.Capture;
using InSpectra.Gen.Engine.Modes.Hook.Execution;
using InSpectra.Gen.Engine.Tooling.Process;
using InSpectra.Gen.Engine.Tests.TestSupport;

using System.Text.Json;

public sealed class HookInstalledToolAnalysisSupportRuntimeCompatibilityTests
{
    private const string MissingSharedRuntimeMessage =
        """
        You must install or update .NET to run this application.

        Framework: 'Microsoft.NETCore.App', version '6.0.0' (x64)
        The following frameworks were found:
          8.0.15 at [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
        """;

    [Fact]
    public async Task AnalyzeInstalledAsync_Retries_With_DotnetRollForward_When_Shared_Runtime_Is_Missing()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var hookDllPath = HookInstalledToolAnalysisTestSupport.CreateHookPlaceholder(tempDirectory.Path);
        var installedTool = HookInstalledToolAnalysisTestSupport.CreateInstalledTool(tempDirectory);
        var invocationCount = 0;
        var runtime = new HookInstalledToolAnalysisTestSupport.FakeHookCommandRuntime(invocation =>
        {
            invocationCount++;
            if (invocationCount == 1)
            {
                Assert.False(invocation.Environment.ContainsKey(HookInstalledToolAnalysisSupport.DotnetRollForwardEnvironmentVariableName));
                return CreateMissingRuntimeResult();
            }

            Assert.Equal(2, invocationCount);
            Assert.Equal(
                HookInstalledToolAnalysisSupport.DotnetRollForwardMajorValue,
                invocation.Environment[HookInstalledToolAnalysisSupport.DotnetRollForwardEnvironmentVariableName]);

            var capturePath = invocation.Environment["INSPECTRA_CAPTURE_PATH"];
            File.WriteAllText(capturePath, JsonSerializer.Serialize(new HookCaptureResult
            {
                CaptureVersion = 1,
                Status = "ok",
                CliFramework = "Microsoft.Extensions.CommandLineUtils",
                FrameworkVersion = "2.2.0.0",
                PatchTarget = "Execute-postfix",
                Root = HookInstalledToolAnalysisTestSupport.CreateValidRootCommand(),
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
        var result = HookInstalledToolAnalysisTestSupport.CreateInitialResult("Microsoft.Extensions.CommandLineUtils");

        await support.AnalyzeInstalledAsync(
            new InstalledToolAnalysisRequest(result, "1.2.3", "demo", tempDirectory.Path, installedTool, tempDirectory.Path, 30),
            CancellationToken.None);

        Assert.Equal(2, invocationCount);
        Assert.Equal("success", result["disposition"]?.GetValue<string>());
        Assert.Equal("startup-hook", result["classification"]?.GetValue<string>());
        Assert.Equal("startup-hook", result["opencliSource"]?.GetValue<string>());
    }

    [Fact]
    public async Task AnalyzeInstalledAsync_Marks_Exhausted_Shared_Runtime_Failures_As_Terminal()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var hookDllPath = HookInstalledToolAnalysisTestSupport.CreateHookPlaceholder(tempDirectory.Path);
        var installedTool = HookInstalledToolAnalysisTestSupport.CreateInstalledTool(tempDirectory);
        var invocationCount = 0;
        var runtime = new HookInstalledToolAnalysisTestSupport.FakeHookCommandRuntime(invocation =>
        {
            invocationCount++;
            if (invocationCount == 1)
            {
                Assert.False(invocation.Environment.ContainsKey(HookInstalledToolAnalysisSupport.DotnetRollForwardEnvironmentVariableName));
            }
            else
            {
                Assert.Equal(
                    HookInstalledToolAnalysisSupport.DotnetRollForwardMajorValue,
                    invocation.Environment[HookInstalledToolAnalysisSupport.DotnetRollForwardEnvironmentVariableName]);
            }

            return CreateMissingRuntimeResult();
        });
        var support = new HookInstalledToolAnalysisSupport(runtime, () => hookDllPath);
        var result = HookInstalledToolAnalysisTestSupport.CreateInitialResult();

        await support.AnalyzeInstalledAsync(
            new InstalledToolAnalysisRequest(result, "1.2.3", "demo", tempDirectory.Path, installedTool, tempDirectory.Path, 30),
            CancellationToken.None);

        Assert.Equal(2, invocationCount);
        Assert.Equal("terminal-failure", result["disposition"]?.GetValue<string>());
        Assert.Equal("hook-runtime-blocked", result["classification"]?.GetValue<string>());
        Assert.Contains("DOTNET_ROLL_FORWARD=Major", result["failureMessage"]?.GetValue<string>(), StringComparison.Ordinal);
    }

    private static CommandRuntime.ProcessResult CreateMissingRuntimeResult()
        => new(
            Status: "failed",
            TimedOut: false,
            ExitCode: -2147450730,
            DurationMs: 15,
            Stdout: string.Empty,
            Stderr: MissingSharedRuntimeMessage);
}
