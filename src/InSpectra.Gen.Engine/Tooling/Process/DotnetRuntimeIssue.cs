namespace InSpectra.Gen.Engine.Tooling.Process;


internal sealed record DotnetRuntimeIssue(
    string Command,
    string Mode,
    DotnetRuntimeRequirement? Requirement);
