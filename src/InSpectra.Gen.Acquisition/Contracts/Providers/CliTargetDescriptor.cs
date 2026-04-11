namespace InSpectra.Gen.Acquisition.Contracts.Providers;

/// <summary>
/// Contracts-level description of a materialized CLI target. Carries the inputs that
/// the <see cref="IAcquisitionAnalysisDispatcher"/> needs to run analyzers against an
/// installed tool without forcing the app shell to reach into deep acquisition types.
/// </summary>
public sealed record CliTargetDescriptor(
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
    string? PackageTitle = null,
    string? PackageDescription = null);
