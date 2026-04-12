namespace InSpectra.Lib.Modes.CliFx.Execution;

using InSpectra.Lib.Modes.CliFx.Crawling;
using InSpectra.Lib.Tooling.Process;

internal sealed class CliFxRuntimeCompatibilityDetector
{
    public DotnetRuntimeIssue? Detect(CliFxCaptureSummary capture)
        => DotnetRuntimeCompatibilitySupport.DetectMissingFramework(
            capture.Command,
            capture.Stdout,
            capture.Stderr);
}

