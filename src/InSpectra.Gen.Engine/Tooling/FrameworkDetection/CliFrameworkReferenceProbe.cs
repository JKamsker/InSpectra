namespace InSpectra.Gen.Engine.Tooling.FrameworkDetection;

internal sealed record CliFrameworkReferenceProbe(
    string FrameworkName,
    IReadOnlyList<string> PackageAssemblyNames,
    IReadOnlyList<string> RuntimeAssemblyNames);
