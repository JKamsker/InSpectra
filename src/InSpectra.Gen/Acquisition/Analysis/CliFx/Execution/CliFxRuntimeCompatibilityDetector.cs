namespace InSpectra.Gen.Acquisition.Analysis.CliFx.Execution;

using InSpectra.Gen.Acquisition.Analysis.CliFx.Crawling;
using InSpectra.Gen.Acquisition.Infrastructure.Commands;

internal sealed class CliFxRuntimeCompatibilityDetector
{
    public DotnetRuntimeIssue? Detect(CliFxCaptureSummary capture)
        => DotnetRuntimeCompatibilitySupport.DetectMissingFramework(
            capture.Command,
            capture.Stdout,
            capture.Stderr);
}

