namespace InSpectra.Gen.Acquisition.Modes.CliFx.Execution;

using InSpectra.Gen.Acquisition.Modes.CliFx.Crawling;
using InSpectra.Gen.Acquisition.Tooling.Process;

internal sealed class CliFxRuntimeCompatibilityDetector
{
    public DotnetRuntimeIssue? Detect(CliFxCaptureSummary capture)
        => DotnetRuntimeCompatibilitySupport.DetectMissingFramework(
            capture.Command,
            capture.Stdout,
            capture.Stderr);
}

