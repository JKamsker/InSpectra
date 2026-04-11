namespace InSpectra.Gen.Acquisition.Tests.Hook;

using InSpectra.Gen.Acquisition.Modes.Hook;
using InSpectra.Gen.Acquisition.Modes.Hook.Models;
using InSpectra.Gen.Acquisition.Tooling.Process;
using InSpectra.Gen.Acquisition.Tests.TestSupport;

using System.Text.Json;
using System.Text.Json.Nodes;

public sealed class HookInstalledToolAnalysisSupportTests
{
    [Fact]
    public async Task AnalyzeInstalledAsync_Applies_Missing_Capture_Failure_With_Process_Diagnostics()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var hookDllPath = HookInstalledToolAnalysisTestSupport.CreateHookPlaceholder(tempDirectory.Path);
        var installedTool = HookInstalledToolAnalysisTestSupport.CreateInstalledTool(tempDirectory);
        var runtime = new HookInstalledToolAnalysisTestSupport.FakeHookCommandRuntime(invocation =>
        {
            var capturePath = invocation.Environment["INSPECTRA_CAPTURE_PATH"];
            Assert.Equal(installedTool.CommandPath, invocation.FilePath);
            Assert.Single(invocation.ArgumentList);
            Assert.Equal("--help", invocation.ArgumentList[0]);
            Assert.Equal(hookDllPath, invocation.Environment["DOTNET_STARTUP_HOOKS"]);
            Assert.StartsWith(tempDirectory.Path, capturePath, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(".json", capturePath, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(
                "System.CommandLine",
                invocation.Environment[HookInstalledToolAnalysisSupport.ExpectedCliFrameworkEnvironmentVariableName]);

            return new CommandRuntime.ProcessResult(
                Status: "failed",
                TimedOut: false,
                ExitCode: -532462766,
                DurationMs: 27,
                Stdout: string.Empty,
                Stderr: "Unhandled exception. System.NullReferenceException: boom");
        });
        var support = new HookInstalledToolAnalysisSupport(runtime, () => hookDllPath);
        var result = HookInstalledToolAnalysisTestSupport.CreateInitialResult();

        await support.AnalyzeInstalledAsync(
            new InstalledToolAnalysisRequest(result, "1.2.3", "demo", tempDirectory.Path, installedTool, tempDirectory.Path, 30),
            CancellationToken.None);

        Assert.Equal("retryable-failure", result["disposition"]?.GetValue<string>());
        Assert.Equal("hook-capture", result["phase"]?.GetValue<string>());
        Assert.Equal("hook-no-capture-file", result["classification"]?.GetValue<string>());

        var failureMessage = result["failureMessage"]?.GetValue<string>();
        Assert.NotNull(failureMessage);
        Assert.Contains("Exit code: -532462766.", failureMessage, StringComparison.Ordinal);
        Assert.Contains("stderr: Unhandled exception. System.NullReferenceException: boom", failureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnalyzeInstalledAsync_Writes_OpenCli_Artifact_When_Hook_Capture_Succeeds()
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
                Status = "ok",
                CliFramework = "System.CommandLine",
                FrameworkVersion = "2.0.0",
                SystemCommandLineVersion = "2.0.0",
                PatchTarget = "Parse-postfix",
                Root = new HookCapturedCommand
                {
                    Name = "demo",
                    Description = "Demo CLI",
                    Options =
                    [
                        new HookCapturedOption
                        {
                            Name = "--verbose",
                            Description = "Verbose output.",
                            ValueType = "Boolean",
                            Aliases = ["-v"],
                        },
                    ],
                    Subcommands =
                    [
                        new HookCapturedCommand
                        {
                            Name = "serve",
                            Description = "Start the server.",
                        },
                    ],
                },
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

        Assert.Equal("success", result["disposition"]?.GetValue<string>());
        Assert.Equal("complete", result["phase"]?.GetValue<string>());
        Assert.Equal("startup-hook", result["classification"]?.GetValue<string>());
        Assert.Equal("startup-hook", result["opencliSource"]?.GetValue<string>());
        Assert.Equal("opencli.json", result["artifacts"]?["opencliArtifact"]?.GetValue<string>());

        var openCli = JsonNode.Parse(File.ReadAllText(Path.Combine(tempDirectory.Path, "opencli.json")))!.AsObject();
        Assert.Equal("demo", openCli["info"]?["title"]?.GetValue<string>());
        Assert.Equal("1.2.3", openCli["info"]?["version"]?.GetValue<string>());
        Assert.Equal("startup-hook", openCli["x-inspectra"]?["artifactSource"]?.GetValue<string>());
        Assert.Equal("System.CommandLine", openCli["x-inspectra"]?["hookCapture"]?["cliFramework"]?.GetValue<string>());
        Assert.Contains(openCli["options"]!.AsArray(), option => option?["name"]?.GetValue<string>() == "--verbose");
        Assert.Contains(openCli["commands"]!.AsArray(), command => command?["name"]?.GetValue<string>() == "serve");
    }

    [Fact]
    public async Task AnalyzeInstalledAsync_Uses_A_Fresh_Capture_Path_Per_Run()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var hookDllPath = HookInstalledToolAnalysisTestSupport.CreateHookPlaceholder(tempDirectory.Path);
        var staleCapturePath = Path.Combine(tempDirectory.Path, "inspectra-capture.json");
        File.WriteAllText(staleCapturePath, "{\"status\":\"stale\"}");
        var installedTool = HookInstalledToolAnalysisTestSupport.CreateInstalledTool(tempDirectory);
        var runtime = new HookInstalledToolAnalysisTestSupport.FakeHookCommandRuntime(invocation =>
        {
            Assert.NotEqual(staleCapturePath, invocation.Environment["INSPECTRA_CAPTURE_PATH"]);
            Assert.True(File.Exists(staleCapturePath));
            return new CommandRuntime.ProcessResult(
                Status: "failed",
                TimedOut: false,
                ExitCode: 1,
                DurationMs: 12,
                Stdout: string.Empty,
                Stderr: "boom");
        });
        var support = new HookInstalledToolAnalysisSupport(runtime, () => hookDllPath);
        var result = HookInstalledToolAnalysisTestSupport.CreateInitialResult();

        await support.AnalyzeInstalledAsync(
            new InstalledToolAnalysisRequest(result, "1.2.3", "demo", tempDirectory.Path, installedTool, tempDirectory.Path, 30),
            CancellationToken.None);

        Assert.Equal("hook-no-capture-file", result["classification"]?.GetValue<string>());
    }

    [Fact]
    public async Task AnalyzeInstalledAsync_Uses_Preferred_Entry_Point_For_Framework_Directory()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var hookDllPath = HookInstalledToolAnalysisTestSupport.CreateHookPlaceholder(tempDirectory.Path);
        var entryPointPath = Path.Combine(tempDirectory.Path, "app", "demo.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(entryPointPath)!);
        File.WriteAllText(entryPointPath, string.Empty);
        var installedTool = HookInstalledToolAnalysisTestSupport.CreateInstalledTool(tempDirectory, entryPointPath);
        var runtime = new HookInstalledToolAnalysisTestSupport.FakeHookCommandRuntime(invocation =>
        {
            Assert.Equal(
                Path.GetDirectoryName(entryPointPath),
                invocation.Environment[HookInstalledToolAnalysisSupport.PreferredFrameworkDirectoryEnvironmentVariableName]);

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
                DurationMs: 12,
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
