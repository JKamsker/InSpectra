namespace InSpectra.Lib.Tooling.Process;


internal sealed record DotnetRuntimeIssue(
    string Command,
    string Mode,
    DotnetRuntimeRequirement? Requirement);
