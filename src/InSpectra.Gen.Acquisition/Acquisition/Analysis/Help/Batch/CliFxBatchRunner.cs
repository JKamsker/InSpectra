namespace InSpectra.Gen.Acquisition.Analysis.Help.Batch;

using InSpectra.Gen.Acquisition.Analysis.Help.Models;

using InSpectra.Gen.Acquisition.Analysis.CliFx;

internal sealed class CliFxBatchRunner : ICliFxBatchRunner
{
    private readonly CliFxService _service = new();

    public Task<int> RunAsync(
        HelpBatchItem item,
        string outputRoot,
        string batchId,
        string source,
        HelpBatchTimeouts timeouts,
        CancellationToken cancellationToken)
        => _service.RunQuietAsync(
            item.PackageId,
            item.Version,
            item.CommandName,
            item.CliFramework,
            outputRoot,
            batchId,
            item.Attempt,
            source,
            timeouts.InstallTimeoutSeconds,
            timeouts.AnalysisTimeoutSeconds,
            timeouts.CommandTimeoutSeconds,
            cancellationToken);
}
