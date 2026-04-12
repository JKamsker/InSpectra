namespace InSpectra.Lib.Contracts.Providers;

internal interface IAcquisitionAnalysisDispatcherInternal : IAcquisitionAnalysisDispatcher
{
    Task<AcquisitionAnalysisOutcome> TryAnalyzeAsync(
        CliTargetDescriptor target,
        string? cleanupRoot,
        string mode,
        string? framework,
        int timeoutSeconds,
        CancellationToken cancellationToken);
}
