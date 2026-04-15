namespace InSpectra.Lib.Tooling.Packages;

public sealed record SpectreAssemblyVersionInfo(
    string Path,
    string? AssemblyVersion,
    string? FileVersion,
    string? InformationalVersion);
