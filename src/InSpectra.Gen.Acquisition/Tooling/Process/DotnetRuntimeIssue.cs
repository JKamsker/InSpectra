namespace InSpectra.Gen.Acquisition.Tooling.Process;


internal sealed record DotnetRuntimeIssue(
    string Command,
    string Mode,
    DotnetRuntimeRequirement? Requirement);
