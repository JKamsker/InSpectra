namespace InSpectra.Gen.Engine.Modes.CliFx.Execution;

using InSpectra.Gen.Engine.Modes.CliFx.Crawling;
using InSpectra.Gen.Engine.Tooling.Process;

internal sealed class CliFxRuntimeCompatibilityDetector
{
    public DotnetRuntimeIssue? Detect(CliFxCaptureSummary capture)
        => DotnetRuntimeCompatibilitySupport.DetectMissingFramework(
            capture.Command,
            capture.Stdout,
            capture.Stderr);
}

