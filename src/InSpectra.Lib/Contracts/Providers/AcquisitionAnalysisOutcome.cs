namespace InSpectra.Lib.Contracts.Providers;

/// <summary>
/// Contracts-level outcome of an installed-tool analyzer pass. The app shell uses this
/// to decide whether to accept a mode result or fall through to the next planned mode.
/// </summary>
public sealed record AcquisitionAnalysisOutcome(
    bool Success,
    string Mode,
    string? Framework,
    string? OpenCliJson,
    string? CrawlJson,
    string? FailureClassification,
    string? FailureMessage);
