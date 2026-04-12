namespace InSpectra.Gen.Engine.Tests.Hook;

using System.Text.Json;
using InSpectra.Gen.Engine.Modes.Hook.Capture;
using InSpectra.Gen.Engine.Modes.Hook.Execution;
using InSpectra.Gen.Engine.Tooling.Process;
using InSpectra.Gen.Engine.Tests.TestSupport;

public sealed class HookProcessRetrySupportCompatibilityTests
{
    [Fact]
    public async Task InvokeWithHelpFallbackAsync_Retries_When_RollForward_Reveals_Missing_Icu()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var capturePath = Path.Combine(tempDirectory.Path, "capture.json");
        var invocation = new HookToolProcessInvocation("demo", ["--help"], PreferredAssemblyPath: null);
        var invocationEnvironments = new List<IReadOnlyDictionary<string, string>>();

        var retryResult = await HookProcessRetrySupport.InvokeWithHelpFallbackAsync(
            invocation,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            capturePath,
            (_, environment, _) =>
            {
                invocationEnvironments.Add(new Dictionary<string, string>(environment, StringComparer.OrdinalIgnoreCase));
                if (invocationEnvironments.Count == 1)
                {
                    return Task.FromResult(MissingSharedRuntimeResult());
                }

                if (invocationEnvironments.Count == 2)
                {
                    return Task.FromResult(MissingIcuResult());
                }

                File.WriteAllText(
                    environment["INSPECTRA_CAPTURE_PATH"],
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

        Assert.Equal(3, invocationEnvironments.Count);
        Assert.False(invocationEnvironments[0].ContainsKey(DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName));
        Assert.Equal(
            DotnetRuntimeCompatibilitySupport.DotnetRollForwardMajorValue,
            invocationEnvironments[1][DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName]);
        Assert.Equal("1", invocationEnvironments[2][DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName]);
        Assert.Equal(
            DotnetRuntimeCompatibilitySupport.DotnetRollForwardMajorValue,
            invocationEnvironments[2][DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName]);
        Assert.Equal(capturePath, retryResult.CapturePath);
    }

    [Fact]
    public async Task InvokeWithHelpFallbackAsync_Replays_Previously_Seen_Fallbacks_After_Compatibility_Environment_Changes()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var capturePath = Path.Combine(tempDirectory.Path, "capture.json");
        var invocation = new HookToolProcessInvocation("demo", ["--help"], PreferredAssemblyPath: null);
        var fallbackKeys = HookToolProcessInvocationResolver.BuildHelpFallbackInvocations(invocation)
            .Select(static candidate => string.Join(' ', candidate.ArgumentList))
            .ToArray();
        Assert.True(fallbackKeys.Length >= 2);

        var previouslySeenFallback = fallbackKeys[0];
        var runtimeBlockedFallback = fallbackKeys[1];
        var invocationCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        var retryResult = await HookProcessRetrySupport.InvokeWithHelpFallbackAsync(
            invocation,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            capturePath,
            (candidateInvocation, environment, _) =>
            {
                var key = string.Join(' ', candidateInvocation.ArgumentList);
                invocationCounts[key] = invocationCounts.GetValueOrDefault(key) + 1;
                var hasRollForward = environment.TryGetValue(
                    DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName,
                    out var rollForward)
                    && string.Equals(
                        rollForward,
                        DotnetRuntimeCompatibilitySupport.DotnetRollForwardMajorValue,
                        StringComparison.OrdinalIgnoreCase);

                if (string.Equals(key, "--help", StringComparison.Ordinal))
                {
                    if (!hasRollForward)
                    {
                        return Task.FromResult(RejectedHelpResult());
                    }

                    WriteRejectedHelpCapture(environment["INSPECTRA_CAPTURE_PATH"]);
                    return Task.FromResult(SuccessResult());
                }

                if (!hasRollForward)
                {
                    return Task.FromResult(
                        string.Equals(key, runtimeBlockedFallback, StringComparison.Ordinal)
                            ? MissingSharedRuntimeResult()
                            : RejectedHelpResult());
                }

                if (string.Equals(key, previouslySeenFallback, StringComparison.Ordinal))
                {
                    WriteSuccessfulCapture(environment["INSPECTRA_CAPTURE_PATH"]);
                }
                else
                {
                    WriteRejectedHelpCapture(environment["INSPECTRA_CAPTURE_PATH"]);
                }

                return Task.FromResult(SuccessResult());
            },
            CancellationToken.None);

        Assert.Equal("ok", retryResult.ProcessResult.Status);
        Assert.Equal(2, invocationCounts.GetValueOrDefault(previouslySeenFallback));
    }

    [Fact]
    public async Task InvokeWithHelpFallbackAsync_Retries_When_Invariant_Reveals_Missing_Shared_Runtime()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var capturePath = Path.Combine(tempDirectory.Path, "capture.json");
        var invocation = new HookToolProcessInvocation("demo", ["--help"], PreferredAssemblyPath: null);
        var invocationEnvironments = new List<IReadOnlyDictionary<string, string>>();

        var retryResult = await HookProcessRetrySupport.InvokeWithHelpFallbackAsync(
            invocation,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            capturePath,
            (_, environment, _) =>
            {
                invocationEnvironments.Add(new Dictionary<string, string>(environment, StringComparer.OrdinalIgnoreCase));
                if (invocationEnvironments.Count == 1)
                {
                    return Task.FromResult(MissingIcuResult());
                }

                if (invocationEnvironments.Count == 2)
                {
                    return Task.FromResult(MissingSharedRuntimeResult());
                }

                File.WriteAllText(
                    environment["INSPECTRA_CAPTURE_PATH"],
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

        Assert.Equal(3, invocationEnvironments.Count);
        Assert.Equal("1", invocationEnvironments[1][DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName]);
        Assert.Equal(
            DotnetRuntimeCompatibilitySupport.DotnetRollForwardMajorValue,
            invocationEnvironments[2][DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName]);
        Assert.Equal(capturePath, retryResult.CapturePath);
    }

    [Fact]
    public async Task InvokeWithHelpFallbackAsync_Overrides_Preexisting_Compatibility_Values()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var capturePath = Path.Combine(tempDirectory.Path, "capture.json");
        var invocation = new HookToolProcessInvocation("demo", ["--help"], PreferredAssemblyPath: null);
        var invocationEnvironments = new List<IReadOnlyDictionary<string, string>>();

        var retryResult = await HookProcessRetrySupport.InvokeWithHelpFallbackAsync(
            invocation,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName] = "0",
                [DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName] = "Minor",
            },
            capturePath,
            (_, environment, _) =>
            {
                invocationEnvironments.Add(new Dictionary<string, string>(environment, StringComparer.OrdinalIgnoreCase));
                if (invocationEnvironments.Count == 1)
                {
                    return Task.FromResult(MissingIcuResult());
                }

                if (invocationEnvironments.Count == 2)
                {
                    return Task.FromResult(MissingSharedRuntimeResult());
                }

                WriteSuccessfulCapture(environment["INSPECTRA_CAPTURE_PATH"]);
                return Task.FromResult(SuccessResult());
            },
            CancellationToken.None);

        Assert.Equal(3, invocationEnvironments.Count);
        Assert.Equal("0", invocationEnvironments[0][DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName]);
        Assert.Equal("1", invocationEnvironments[1][DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName]);
        Assert.Equal("Minor", invocationEnvironments[1][DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName]);
        Assert.Equal(
            DotnetRuntimeCompatibilitySupport.DotnetRollForwardMajorValue,
            invocationEnvironments[2][DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName]);
        Assert.Equal(capturePath, retryResult.CapturePath);
    }

    private static CommandRuntime.ProcessResult MissingSharedRuntimeResult()
        => new(
            Status: "failed",
            TimedOut: false,
            ExitCode: 1,
            DurationMs: 1,
            Stdout: string.Empty,
            Stderr:
            """
            You must install or update .NET to run this application.

            Framework: 'Microsoft.NETCore.App', version '9.0.0' (x64)
            """);

    private static CommandRuntime.ProcessResult MissingIcuResult()
        => new(
            Status: "failed",
            TimedOut: false,
            ExitCode: 1,
            DurationMs: 1,
            Stdout: string.Empty,
            Stderr: "Couldn't find a valid ICU package installed on the system.");

    private static CommandRuntime.ProcessResult RejectedHelpResult()
        => new(
            Status: "failed",
            TimedOut: false,
            ExitCode: 1,
            DurationMs: 1,
            Stdout: string.Empty,
            Stderr: "error: Unrecognized command or argument '--help'.");

    private static CommandRuntime.ProcessResult SuccessResult()
        => new(
            Status: "ok",
            TimedOut: false,
            ExitCode: 0,
            DurationMs: 1,
            Stdout: string.Empty,
            Stderr: string.Empty);

    private static void WriteRejectedHelpCapture(string capturePath)
        => File.WriteAllText(
            capturePath,
            JsonSerializer.Serialize(new HookCaptureResult
            {
                CaptureVersion = 1,
                Status = "failed",
                Error = "error: Unrecognized command or argument '--help'.",
            }));

    private static void WriteSuccessfulCapture(string capturePath)
        => File.WriteAllText(
            capturePath,
            JsonSerializer.Serialize(new HookCaptureResult
            {
                CaptureVersion = 1,
                Status = "ok",
                Root = new HookCapturedCommand
                {
                    Name = "demo",
                },
            }));
}
