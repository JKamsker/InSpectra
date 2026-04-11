namespace InSpectra.Gen.Acquisition.Tooling.Packages;

internal sealed record SpectrePackageInspection(
    IReadOnlyList<string> DepsFilePaths,
    IReadOnlyList<string> SpectreConsoleDependencyVersions,
    IReadOnlyList<string> SpectreConsoleCliDependencyVersions,
    IReadOnlyList<SpectreAssemblyVersionInfo> SpectreConsoleAssemblies,
    IReadOnlyList<SpectreAssemblyVersionInfo> SpectreConsoleCliAssemblies,
    IReadOnlyList<string> ToolSettingsPaths,
    IReadOnlyList<string> ToolCommandNames,
    IReadOnlyList<string> ToolEntryPointPaths,
    IReadOnlyList<string> ToolAssembliesReferencingSpectreConsole,
    IReadOnlyList<string> ToolAssembliesReferencingSpectreConsoleCli,
    IReadOnlyList<ToolCliFrameworkReferenceInspection> ToolCliFrameworkReferences)
{
    public bool HasToolAssemblyReferencingSpectreConsoleCli => ToolAssembliesReferencingSpectreConsoleCli.Count > 0;

    public bool HasToolAssemblyReferencingCliFramework(string frameworkName)
        => ToolCliFrameworkReferences.Any(reference =>
            string.Equals(reference.FrameworkName, frameworkName, StringComparison.OrdinalIgnoreCase));

    public static SpectrePackageInspection Empty { get; } = new(
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        []);
}

internal sealed record SpectreAssemblyVersionInfo(
    string Path,
    string? AssemblyVersion,
    string? FileVersion,
    string? InformationalVersion);
