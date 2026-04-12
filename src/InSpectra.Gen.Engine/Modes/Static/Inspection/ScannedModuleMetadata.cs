namespace InSpectra.Gen.Engine.Modes.Static.Inspection;

internal sealed record ScannedModuleMetadata(
    string Path,
    string? AssemblyName,
    IReadOnlyList<string> AssemblyReferences);
