namespace InSpectra.Gen.Acquisition.Analysis.NonSpectre;


internal sealed record NonSpectreAnalysisExecutionDefinition(
    string AnalysisMode,
    string TempRootPrefix,
    string TimeoutLabel,
    string? DefaultCliFramework = null,
    bool InitializeCoverage = false);
