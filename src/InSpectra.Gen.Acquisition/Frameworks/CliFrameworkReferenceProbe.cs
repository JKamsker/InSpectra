namespace InSpectra.Gen.Acquisition.Frameworks;

internal sealed record CliFrameworkReferenceProbe(
    string FrameworkName,
    IReadOnlyList<string> PackageAssemblyNames,
    IReadOnlyList<string> RuntimeAssemblyNames);
