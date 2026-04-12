using InSpectra.Gen.Engine.Contracts;
using InSpectra.Gen.Engine.Contracts.Providers;
using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Execution.Process;
using InSpectra.Gen.Engine.Tooling.Process;

namespace InSpectra.Gen.Tests.OpenCli;

internal sealed class FakeProcessRunner : IProcessRunner
{
    public Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => RunAsync(
            executablePath,
            workingDirectory,
            arguments,
            timeoutSeconds,
            environment: null,
            cleanupRoot: null,
            cancellationToken);

    public Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        IReadOnlyDictionary<string, string>? environment,
        string? cleanupRoot,
        CancellationToken cancellationToken)
        => Task.FromResult(new ProcessResult(
            StandardOutput:
            """
            {"openCliVersion":"0.1-draft","info":{"title":"demo","version":"1.0.0"}}
            """,
            StandardError: string.Empty));
}

internal sealed class CapturingProcessRunner : IProcessRunner
{
    public List<string?> CleanupRoots { get; } = [];

    public Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => RunAsync(
            executablePath,
            workingDirectory,
            arguments,
            timeoutSeconds,
            environment: null,
            cleanupRoot: null,
            cancellationToken);

    public Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        IReadOnlyDictionary<string, string>? environment,
        string? cleanupRoot,
        CancellationToken cancellationToken)
    {
        CleanupRoots.Add(cleanupRoot);
        return Task.FromResult(new ProcessResult(
            StandardOutput:
            """
            {"openCliVersion":"0.1-draft","info":{"title":"demo","version":"1.0.0"}}
            """,
            StandardError: string.Empty));
    }
}

internal sealed class InvalidJsonProcessRunner(string standardOutput) : IProcessRunner
{
    public Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => RunAsync(
            executablePath,
            workingDirectory,
            arguments,
            timeoutSeconds,
            environment: null,
            cleanupRoot: null,
            cancellationToken);

    public Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        IReadOnlyDictionary<string, string>? environment,
        string? cleanupRoot,
        CancellationToken cancellationToken)
        => Task.FromResult(new ProcessResult(
            StandardOutput: standardOutput,
            StandardError: string.Empty));
}

internal sealed class FakeLocalCliFrameworkDetector : ILocalCliFrameworkDetector
{
    public LocalCliFrameworkDetection Detect(string installDirectory)
        => new(null, null, HasManagedAssemblies: false);
}

internal sealed class EmptyCliFrameworkCatalog : ICliFrameworkCatalog
{
    public string? CombineFrameworkNames(IEnumerable<string> frameworkNames)
        => null;

    public IReadOnlyList<CliFrameworkCatalogEntry> GetAllFrameworks()
        => [];

    public IReadOnlyList<CliFrameworkCatalogEntry> ResolveAnalysisProviders(string? cliFramework)
        => [];

    public IReadOnlyList<string> ResolveFrameworkNames(string? cliFramework)
        => [];
}

internal sealed class SuccessfulDispatcher(string openCliJson, string? crawlJson) : IAcquisitionAnalysisDispatcherInternal
{
    public Task<AcquisitionAnalysisOutcome> TryAnalyzeAsync(
        CliTargetDescriptor target,
        string mode,
        string? framework,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => TryAnalyzeAsync(
            target,
            cleanupRoot: null,
            mode,
            framework,
            timeoutSeconds,
            cancellationToken);

    public Task<AcquisitionAnalysisOutcome> TryAnalyzeAsync(
        CliTargetDescriptor target,
        string? cleanupRoot,
        string mode,
        string? framework,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => Task.FromResult(new AcquisitionAnalysisOutcome(
            Success: true,
            Mode: mode,
            Framework: framework,
            OpenCliJson: openCliJson,
            CrawlJson: crawlJson,
            FailureClassification: null,
            FailureMessage: null));
}

internal sealed class CapturingDispatcher(string openCliJson, string? crawlJson) : IAcquisitionAnalysisDispatcherInternal
{
    public string? LastCleanupRoot { get; private set; }

    public Task<AcquisitionAnalysisOutcome> TryAnalyzeAsync(
        CliTargetDescriptor target,
        string mode,
        string? framework,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => TryAnalyzeAsync(
            target,
            cleanupRoot: null,
            mode,
            framework,
            timeoutSeconds,
            cancellationToken);

    public Task<AcquisitionAnalysisOutcome> TryAnalyzeAsync(
        CliTargetDescriptor target,
        string? cleanupRoot,
        string mode,
        string? framework,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        LastCleanupRoot = cleanupRoot;
        return Task.FromResult(new AcquisitionAnalysisOutcome(
            Success: true,
            Mode: mode,
            Framework: framework,
            OpenCliJson: openCliJson,
            CrawlJson: crawlJson,
            FailureClassification: null,
            FailureMessage: null));
    }
}

internal sealed class UnusedPackageInstaller : IPackageCliToolInstaller
{
    public Task<PackageCliToolInstallation> InstallAsync(
        string packageId,
        string version,
        string? commandName,
        string? cliFramework,
        string tempRoot,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();
}

internal sealed class RecordingPackageInstaller : IPackageCliToolInstaller
{
    public string? LastTempRoot { get; private set; }

    public Task<PackageCliToolInstallation> InstallAsync(
        string packageId,
        string version,
        string? commandName,
        string? cliFramework,
        string tempRoot,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        LastTempRoot = tempRoot;
        var installDirectory = Path.Combine(tempRoot, "tool");
        Directory.CreateDirectory(installDirectory);
        var commandPath = Path.Combine(installDirectory, "demo.cmd");
        File.WriteAllText(commandPath, "@echo off");
        var sandboxEnvironment = new CommandRuntime().CreateSandboxEnvironment(tempRoot);

        return Task.FromResult(new PackageCliToolInstallation(
            PackageId: packageId,
            Version: version,
            CommandName: commandName ?? "demo",
            CommandPath: commandPath,
            InstallDirectory: installDirectory,
            PreferredEntryPointPath: null,
            Environment: sandboxEnvironment.Values,
            CliFramework: cliFramework,
            HookCliFramework: cliFramework,
            PackageTitle: null,
            PackageDescription: null));
    }
}

internal sealed class ThrowingAcquisitionProcessRunner(CliException exception) : IProcessRunner
{
    public Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => Task.FromException<ProcessResult>(exception);

    public Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        IReadOnlyDictionary<string, string>? environment,
        string? cleanupRoot,
        CancellationToken cancellationToken)
        => Task.FromException<ProcessResult>(exception);
}

internal sealed class AlwaysFailingDispatcher : IAcquisitionAnalysisDispatcherInternal
{
    public Task<AcquisitionAnalysisOutcome> TryAnalyzeAsync(
        CliTargetDescriptor target,
        string mode,
        string? framework,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => TryAnalyzeAsync(target, null, mode, framework, timeoutSeconds, cancellationToken);

    public Task<AcquisitionAnalysisOutcome> TryAnalyzeAsync(
        CliTargetDescriptor target,
        string? cleanupRoot,
        string mode,
        string? framework,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => Task.FromResult(new AcquisitionAnalysisOutcome(
            Success: false,
            Mode: mode,
            Framework: framework,
            OpenCliJson: null,
            CrawlJson: null,
            FailureClassification: "analysis_failed",
            FailureMessage: $"{mode} mode failed"));
}
