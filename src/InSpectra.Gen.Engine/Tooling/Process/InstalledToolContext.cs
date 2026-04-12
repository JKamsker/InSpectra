namespace InSpectra.Gen.Engine.Tooling.Process;

internal sealed record InstalledToolContext(
    IReadOnlyDictionary<string, string> Environment,
    string InstallDirectory,
    string CommandPath,
    string? PreferredEntryPointPath = null,
    string? CleanupRoot = null);
