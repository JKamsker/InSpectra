namespace InSpectra.Gen.Acquisition.Tooling.Packages;

internal sealed record ToolCliFrameworkReferenceInspection(
    string FrameworkName,
    IReadOnlyList<string> ReferencingAssemblyPaths);
