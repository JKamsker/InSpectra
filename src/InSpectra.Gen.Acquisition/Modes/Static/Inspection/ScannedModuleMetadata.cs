namespace InSpectra.Gen.Acquisition.Modes.Static.Inspection;

internal sealed record ScannedModuleMetadata(
    string Path,
    string? AssemblyName,
    IReadOnlyList<string> AssemblyReferences);
