namespace InSpectra.Gen.Acquisition.Analysis.Auto.Runners;

internal interface IAutoNativeRunner
{
    Task RunAsync(
        string packageId,
        string version,
        string outputRoot,
        string batchId,
        int attempt,
        string source,
        int installTimeoutSeconds,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken);
}
