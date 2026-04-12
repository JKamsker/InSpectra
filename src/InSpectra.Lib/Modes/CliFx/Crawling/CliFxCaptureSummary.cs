namespace InSpectra.Lib.Modes.CliFx.Crawling;

internal sealed record CliFxCaptureSummary(
    string Command,
    string? HelpSwitch,
    bool Parsed,
    bool TimedOut,
    int? ExitCode,
    string? Stdout,
    string? Stderr,
    bool OutputLimitExceeded = false,
    string? GuardrailFailureMessage = null);
