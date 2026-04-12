using InSpectra.Gen.Engine.Contracts;
using InSpectra.Gen.Engine.Contracts.Providers;
using InSpectra.Gen.Engine.Execution.Process;

namespace InSpectra.Gen.Tests.OpenCli;

internal sealed class FakeProcessRunner : IProcessRunner
{
    public Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => RunAsync(executablePath, workingDirectory, arguments, timeoutSeconds, environment: null, cancellationToken);

    public Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken)
        => Task.FromResult(new ProcessResult(
            StandardOutput:
            """
            {"openCliVersion":"0.1-draft","info":{"title":"demo","version":"1.0.0"}}
            """,
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

internal sealed class SuccessfulDispatcher(string openCliJson, string? crawlJson) : IAcquisitionAnalysisDispatcher
{
    public Task<AcquisitionAnalysisOutcome> TryAnalyzeAsync(
        CliTargetDescriptor target,
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
