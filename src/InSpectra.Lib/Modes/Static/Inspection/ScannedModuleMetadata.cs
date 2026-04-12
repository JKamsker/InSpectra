namespace InSpectra.Lib.Modes.Static.Inspection;

internal sealed record ScannedModuleMetadata(
    string Path,
    string? AssemblyName,
    IReadOnlyList<string> AssemblyReferences);
