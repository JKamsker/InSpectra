namespace InSpectra.Gen.Engine.Targets.Sources;

internal sealed record MaterializedCliTarget(
    string DisplayName,
    string CommandPath,
    string CommandName,
    string WorkingDirectory,
    string InstallDirectory,
    string? PreferredEntryPointPath,
    string Version,
    IReadOnlyDictionary<string, string> Environment,
    string? CliFramework,
    string? HookCliFramework,
    string? CleanupRoot = null,
    string? XmlDocCommandPath = null,
    string? PackageTitle = null,
    string? PackageDescription = null);
