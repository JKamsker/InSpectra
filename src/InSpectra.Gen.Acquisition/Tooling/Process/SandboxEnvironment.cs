namespace InSpectra.Gen.Acquisition.Tooling.Process;


internal sealed record SandboxEnvironment(
    IReadOnlyDictionary<string, string> Values,
    IReadOnlyList<string> Directories);
