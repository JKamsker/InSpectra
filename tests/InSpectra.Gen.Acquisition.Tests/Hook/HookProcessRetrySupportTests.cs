namespace InSpectra.Gen.Acquisition.Tests.Hook;

using InSpectra.Gen.Acquisition.Modes.Hook.Execution;
using InSpectra.Gen.Acquisition.Tooling.Process;
using InSpectra.Gen.Acquisition.Tests.TestSupport;

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

        Assert.Equal(2, invocationCounts["--help"]);
        foreach (var fallbackKey in fallbackKeys)
        {
            Assert.Equal(1, invocationCounts[fallbackKey]);
        }
    }
}
