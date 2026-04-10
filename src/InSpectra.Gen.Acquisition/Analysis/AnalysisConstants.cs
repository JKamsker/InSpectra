namespace InSpectra.Gen.Acquisition.Analysis;

internal static class AnalysisMode
{
    public const string Help = "help";
    public const string CliFx = "clifx";
    public const string Static = "static";
    public const string Hook = "hook";
    public const string Native = "native";
    public const string Auto = "auto";
}

internal static class AnalysisDisposition
{
    public const string Success = "success";
    public const string Failed = "failed";
    public const string Planned = "planned";
    public const string Skipped = "skipped";
    public const string RetryableFailure = "retryable-failure";
    public const string TerminalFailure = "terminal-failure";
    public const string TerminalNegative = "terminal-negative";
}

internal static class ResultKey
{
    public const string Disposition = "disposition";
    public const string Classification = "classification";
    public const string FailureMessage = "failureMessage";
    public const string AnalysisMode = "analysisMode";
    public const string CliFramework = "cliFramework";
    public const string NugetTitle = "nugetTitle";
    public const string NugetDescription = "nugetDescription";
    public const string BatchId = "batchId";
    public const string Attempt = "attempt";
    public const string Source = "source";
    public const string AnalyzedAt = "analyzedAt";
}
