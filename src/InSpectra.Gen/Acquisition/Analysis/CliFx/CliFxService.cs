namespace InSpectra.Gen.Acquisition.Analysis.CliFx;

using InSpectra.Gen.Acquisition.Infrastructure.Host;

using InSpectra.Gen.Acquisition.Analysis.CliFx.OpenCli;

using InSpectra.Gen.Acquisition.Analysis.CliFx.Metadata;

using InSpectra.Gen.Acquisition.Analysis.CliFx.Execution;

using InSpectra.Gen.Acquisition.Analysis.NonSpectre;

using InSpectra.Discovery.Tool.Analysis;

internal sealed class CliFxService
{
    private static readonly NonSpectreAnalysisExecutionDefinition Definition = new(
        AnalysisMode: AnalysisMode.CliFx,
        TempRootPrefix: "inspectra-clifx",
        TimeoutLabel: "CliFx analysis",
        DefaultCliFramework: "CliFx",
        InitializeCoverage: true);

    private readonly CliFxRuntime _runtime = new();
    private readonly CliFxInstalledToolAnalysisSupport _installedToolAnalyzer;

    public CliFxService()
    {
        _installedToolAnalyzer = new CliFxInstalledToolAnalysisSupport(
            _runtime,
            new CliFxMetadataInspector(),
            new CliFxOpenCliBuilder(),
            new CliFxCoverageClassifier());
    }

    public Task<int> RunQuietAsync(
        string packageId,
        string version,
        string? commandName,
        string? cliFramework,
        string outputRoot,
        string batchId,
        int attempt,
        string source,
        int installTimeoutSeconds,
        int analysisTimeoutSeconds,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
        => NonSpectreExecutionSupport.RunQuietAsync(
            _runtime,
            Definition,
            BootstrapAsync,
            AnalyzeInstalledToolAsync,
            packageId,
            version,
            commandName,
            cliFramework,
            outputRoot,
            batchId,
            attempt,
            source,
            installTimeoutSeconds,
            analysisTimeoutSeconds,
            commandTimeoutSeconds,
            cancellationToken);

    public Task<int> RunAsync(
        string packageId,
        string version,
        string? commandName,
        string? cliFramework,
        string outputRoot,
        string batchId,
        int attempt,
        string source,
        int installTimeoutSeconds,
        int analysisTimeoutSeconds,
        int commandTimeoutSeconds,
        bool json,
        CancellationToken cancellationToken)
        => NonSpectreExecutionSupport.RunAsync(
            _runtime,
            Definition,
            BootstrapAsync,
            AnalyzeInstalledToolAsync,
            packageId,
            version,
            commandName,
            cliFramework,
            outputRoot,
            batchId,
            attempt,
            source,
            installTimeoutSeconds,
            analysisTimeoutSeconds,
            commandTimeoutSeconds,
            json,
            cancellationToken);

    private static async Task<NonSpectreAnalysisBootstrapResult> BootstrapAsync(
        System.Text.Json.Nodes.JsonObject result,
        string packageId,
        string version,
        string? commandName,
        CancellationToken cancellationToken)
    {
        using var scope = Runtime.CreateNuGetApiClientScope();
        return await NonSpectreBootstrapSupport.PopulateResultAsync(
            result,
            scope.Client,
            packageId,
            version,
            commandName,
            cancellationToken);
    }

    private Task AnalyzeInstalledToolAsync(NonSpectreInstalledToolAnalysisRequest request, CancellationToken cancellationToken)
        => _installedToolAnalyzer.AnalyzeAsync(
            request.Result,
            request.PackageId,
            request.Version,
            request.CommandName,
            request.OutputDirectory,
            request.TempRoot,
            request.InstallTimeoutSeconds,
            request.CommandTimeoutSeconds,
            cancellationToken);
}


