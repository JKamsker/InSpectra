namespace InSpectra.Lib.Tests.Hook;

using InSpectra.Lib.Modes.Hook.Capture;
using InSpectra.Lib.Modes.Hook.Execution;
using InSpectra.Lib.Tooling.Process;
using InSpectra.Lib.Tests.TestSupport;

using System.Text.Json;

public sealed class HookProcessRetrySupportTests
{
    [Fact]
    public async Task InvokeWithHelpFallbackAsync_Does_Not_Revisit_Help_Variants_After_Compatibility_Retries()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var capturePath = Path.Combine(tempDirectory.Path, "capture.json");
        var invocation = new HookToolProcessInvocation("demo", ["--help"], PreferredAssemblyPath: null);
        var invocationCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        await HookProcessRetrySupport.InvokeWithHelpFallbackAsync(
            invocation,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            capturePath,
            (candidateInvocation, environment, _) =>
            {
                var key = string.Join(' ', candidateInvocation.ArgumentList);
                invocationCounts[key] = invocationCounts.GetValueOrDefault(key) + 1;

                var stderr = environment.ContainsKey(DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName)
                    ? "error: Unrecognized command or argument '--help'."
                    : "Couldn't find a valid ICU package installed on the system.";
                return Task.FromResult(new CommandRuntime.ProcessResult(
                    Status: "failed",
                    TimedOut: false,
                    ExitCode: 1,
                    DurationMs: 1,
                    Stdout: string.Empty,
                    Stderr: stderr));
            },
            CancellationToken.None);

        var fallbackKeys = HookToolProcessInvocationResolver.BuildHelpFallbackInvocations(invocation)
            .Select(static candidate => string.Join(' ', candidate.ArgumentList))
            .ToArray();

        Assert.True(invocationCounts["--help"] >= 2);
        foreach (var fallbackKey in fallbackKeys)
        {
            Assert.True(invocationCounts[fallbackKey] >= 1);
        }
    }

    [Fact]
    public async Task InvokeWithHelpFallbackAsync_Uses_A_Fresh_Capture_File_When_A_Retry_Replaces_A_Rejected_Capture()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var capturePath = Path.Combine(tempDirectory.Path, "capture.json");
        var invocation = new HookToolProcessInvocation("demo", ["--help"], PreferredAssemblyPath: null);
        var capturePaths = new List<string>();
        var fallbackInvocation = HookToolProcessInvocationResolver.BuildHelpFallbackInvocations(invocation).First();
        FileStream? lockedCapture = null;

        try
        {
            var retryResult = await HookProcessRetrySupport.InvokeWithHelpFallbackAsync(
                invocation,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["INSPECTRA_CAPTURE_PATH"] = capturePath,
                },
                capturePath,
                (candidateInvocation, environment, _) =>
                {
                    var attemptCapturePath = environment["INSPECTRA_CAPTURE_PATH"];
                    capturePaths.Add(attemptCapturePath);
                    var invocationKey = string.Join(' ', candidateInvocation.ArgumentList);

                    if (string.Equals(invocationKey, "--help", StringComparison.Ordinal))
                    {
                        File.WriteAllText(
                            attemptCapturePath,
                            JsonSerializer.Serialize(new HookCaptureResult
                            {
                                CaptureVersion = 1,
                                Status = "failed",
                                Error = "error: Unrecognized command or argument '--help'.",
                            }));
                        lockedCapture = new FileStream(
                            attemptCapturePath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read);
                        return Task.FromResult(new CommandRuntime.ProcessResult(
                            Status: "failed",
                            TimedOut: false,
                            ExitCode: 1,
                            DurationMs: 1,
                            Stdout: string.Empty,
                            Stderr: string.Empty));
                    }

                    Assert.Equal(string.Join(' ', fallbackInvocation.ArgumentList), invocationKey);
                    Assert.NotEqual(capturePaths[0], attemptCapturePath);
                    File.WriteAllText(
                        attemptCapturePath,
                        JsonSerializer.Serialize(new HookCaptureResult
                        {
                            CaptureVersion = 1,
                            Status = "ok",
                            Root = new HookCapturedCommand
                            {
                                Name = "demo",
                            },
                        }));
                    return Task.FromResult(new CommandRuntime.ProcessResult(
                        Status: "ok",
                        TimedOut: false,
                        ExitCode: 0,
                        DurationMs: 1,
                        Stdout: string.Empty,
                        Stderr: string.Empty));
                },
                CancellationToken.None);

            Assert.Equal("ok", retryResult.ProcessResult.Status);
            Assert.Equal(capturePath, retryResult.CapturePath);
            Assert.Equal(2, capturePaths.Count);
            Assert.NotEqual(capturePaths[0], capturePaths[1]);
            Assert.True(File.Exists(capturePath));

            var capture = HookCaptureDeserializer.Deserialize(capturePath);
            Assert.NotNull(capture);
            Assert.Equal("ok", capture.Status);
            Assert.Equal("demo", capture.Root?.Name);
        }
        finally
        {
            lockedCapture?.Dispose();
        }
    }

    [Fact]
    public async Task InvokeWithHelpFallbackAsync_Preserves_Attempt_Capture_When_Final_Publication_Fails()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var capturePath = Path.Combine(tempDirectory.Path, "capture.json");
        Directory.CreateDirectory(capturePath);
        var invocation = new HookToolProcessInvocation("demo", ["--help"], PreferredAssemblyPath: null);
        string? attemptCapturePath = null;

        var retryResult = await HookProcessRetrySupport.InvokeWithHelpFallbackAsync(
            invocation,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            capturePath,
            (_, environment, _) =>
            {
                attemptCapturePath = environment["INSPECTRA_CAPTURE_PATH"];
                File.WriteAllText(
                    attemptCapturePath,
                    JsonSerializer.Serialize(new HookCaptureResult
                    {
                        CaptureVersion = 1,
                        Status = "ok",
                        Root = new HookCapturedCommand
                        {
                            Name = "demo",
                        },
                    }));
                return Task.FromResult(new CommandRuntime.ProcessResult(
                    Status: "ok",
                    TimedOut: false,
                    ExitCode: 0,
                    DurationMs: 1,
                    Stdout: string.Empty,
                    Stderr: string.Empty));
            },
            CancellationToken.None);

        Assert.Equal("ok", retryResult.ProcessResult.Status);
        Assert.NotNull(attemptCapturePath);
        Assert.Equal(attemptCapturePath, retryResult.CapturePath);
        Assert.True(File.Exists(attemptCapturePath));
        Assert.True(Directory.Exists(capturePath));

        var capture = HookCaptureDeserializer.Deserialize(retryResult.CapturePath!);
        Assert.NotNull(capture);
        Assert.Equal("ok", capture.Status);
        Assert.Equal("demo", capture.Root?.Name);
    }

}
