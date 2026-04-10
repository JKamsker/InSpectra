namespace InSpectra.Gen.Acquisition.Analysis.Help.Models;


internal sealed record HelpBatchTimeouts(
    int InstallTimeoutSeconds,
    int AnalysisTimeoutSeconds,
    int CommandTimeoutSeconds);
