namespace InSpectra.Gen.Acquisition.Tests.Hook;

using InSpectra.Gen.Acquisition.Modes.Hook;
using InSpectra.Gen.Acquisition.Modes.Hook.Models;
using InSpectra.Gen.Acquisition.Tooling.Process;
using InSpectra.Gen.Acquisition.Tooling.Results;
using InSpectra.Gen.Acquisition.Tests.TestSupport;

using System.Text.Json.Nodes;

internal static class HookInstalledToolAnalysisTestSupport
{
    public static JsonObject CreateInitialResult(string cliFramework = "System.CommandLine")
        => NonSpectreResultSupport.CreateInitialResult(
            packageId: "Demo.Tool",
            version: "1.2.3",
            commandName: "demo",
            batchId: "batch-001",
            attempt: 1,
            source: "unit-test",
            cliFramework: cliFramework,
            analysisMode: "hook",
            analyzedAt: DateTimeOffset.Parse("2026-03-31T00:00:00Z"));

    public static HookCapturedCommand CreateValidRootCommand()
        => new()
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
                },
            ],
        };

    public static string CreateHookPlaceholder(string tempRoot)
    {
        var hookPath = Path.Combine(tempRoot, "hooks", "InSpectra.Gen.StartupHook.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(hookPath)!);
        File.WriteAllText(hookPath, string.Empty);
        return hookPath;
    }

    public static InstalledToolContext CreateInstalledTool(
        RepositoryRegressionTestSupport.TemporaryDirectory tempDirectory,
        string? preferredEntryPointPath = null)
    {
        var installDirectory = Path.Combine(tempDirectory.Path, "tool");
        Directory.CreateDirectory(installDirectory);
        var commandPath = Path.Combine(installDirectory, "demo.cmd");
        File.WriteAllText(commandPath, "@echo off");

        return new InstalledToolContext(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            installDirectory,
            commandPath,
            preferredEntryPointPath);
    }

    internal sealed class FakeHookCommandRuntime(
        Func<HookInvocation, CommandRuntime.ProcessResult> hookHandler) : CommandRuntime
    {
        public override Task<ProcessResult> InvokeProcessCaptureAsync(
            string filePath,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environment,
            int timeoutSeconds,
            string? sandboxRoot,
            CancellationToken cancellationToken)
        {
            var invocation = new HookInvocation(
                filePath,
                argumentList.ToArray(),
                new Dictionary<string, string>(environment, StringComparer.OrdinalIgnoreCase),
                workingDirectory,
                sandboxRoot);
            return Task.FromResult(hookHandler(invocation));
        }
    }

    internal sealed record HookInvocation(
        string FilePath,
        string[] ArgumentList,
        IReadOnlyDictionary<string, string> Environment,
        string WorkingDirectory,
        string? SandboxRoot);
}
