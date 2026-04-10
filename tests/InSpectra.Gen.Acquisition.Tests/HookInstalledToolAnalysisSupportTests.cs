namespace InSpectra.Gen.Acquisition.Tests;

using InSpectra.Discovery.Tool.Analysis.Hook;
using InSpectra.Discovery.Tool.Infrastructure.Commands;

using System.Text.Json;
using System.Text.Json.Nodes;

public sealed class HookInstalledToolAnalysisSupportTests
{
    [Fact]
    public async Task AnalyzeInstalledAsync_Applies_Missing_Capture_Failure_With_Process_Diagnostics()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var hookDllPath = HookInstalledToolAnalysisTestSupport.CreateHookPlaceholder(tempDirectory.Path);
        var capturePath = Path.Combine(tempDirectory.Path, "inspectra-capture.json");
        var installedTool = HookInstalledToolAnalysisTestSupport.CreateInstalledTool(tempDirectory);
        var runtime = new HookInstalledToolAnalysisTestSupport.FakeHookCommandRuntime(invocation =>
        {
            Assert.Equal(installedTool.CommandPath, invocation.FilePath);
            Assert.Single(invocation.ArgumentList);
            Assert.Equal("--help", invocation.ArgumentList[0]);
            Assert.Equal(hookDllPath, invocation.Environment["DOTNET_STARTUP_HOOKS"]);
            Assert.Equal(capturePath, invocation.Environment["INSPECTRA_CAPTURE_PATH"]);
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
            result,
            version: "1.2.3",
            commandName: "demo",
            outputDirectory: tempDirectory.Path,
            installedTool: installedTool,
            workingDirectory: tempDirectory.Path,
            commandTimeoutSeconds: 30,
            cancellationToken: CancellationToken.None);

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
            result,
            version: "1.2.3",
            commandName: "demo",
            outputDirectory: tempDirectory.Path,
            installedTool: installedTool,
            workingDirectory: tempDirectory.Path,
            commandTimeoutSeconds: 30,
            cancellationToken: CancellationToken.None);

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
                return new CommandRuntime.ProcessResult(
                    Status: "failed",
                    TimedOut: false,
                    ExitCode: -2147450730,
                    DurationMs: 15,
                    Stdout: string.Empty,
                    Stderr:
                    """
                    You must install or update .NET to run this application.

                    Framework: 'Microsoft.NETCore.App', version '6.0.0' (x64)
                    The following frameworks were found:
                      8.0.15 at [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """);
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
            result,
            version: "1.2.3",
            commandName: "demo",
            outputDirectory: tempDirectory.Path,
            installedTool: installedTool,
            workingDirectory: tempDirectory.Path,
            commandTimeoutSeconds: 30,
            cancellationToken: CancellationToken.None);

        Assert.Equal(2, invocationCount);
        Assert.Equal("success", result["disposition"]?.GetValue<string>());
        Assert.Equal("startup-hook", result["classification"]?.GetValue<string>());
        Assert.Equal("startup-hook", result["opencliSource"]?.GetValue<string>());
    }
}
