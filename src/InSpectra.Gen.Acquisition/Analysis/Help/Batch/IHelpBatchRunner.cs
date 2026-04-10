namespace InSpectra.Gen.Acquisition.Analysis.Help.Batch;

using InSpectra.Gen.Acquisition.Analysis.Help.Models;

internal interface IHelpBatchRunner
{
    Task<int> RunAsync(
        HelpBatchItem item,
        string outputRoot,
        string batchId,
        string source,
        HelpBatchTimeouts timeouts,
        CancellationToken cancellationToken);
}
