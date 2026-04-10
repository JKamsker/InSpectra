namespace InSpectra.Gen.Acquisition.Analysis.Execution;


internal sealed record SandboxEnvironment(
    IReadOnlyDictionary<string, string> Values,
    IReadOnlyList<string> Directories);
