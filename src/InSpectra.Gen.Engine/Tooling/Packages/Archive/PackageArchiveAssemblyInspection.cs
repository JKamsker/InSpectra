namespace InSpectra.Gen.Engine.Tooling.Packages.Archive;

internal sealed record PackageArchiveAssemblyInspection(
    string Path,
    string? AssemblyVersion,
    string? FileVersion,
    string? InformationalVersion,
    IReadOnlyList<string> AssemblyReferences)
{
    public static PackageArchiveAssemblyInspection Empty(string path) => new(path, null, null, null, []);
}
