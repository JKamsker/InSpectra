namespace InSpectra.Gen.Acquisition.Packages;

internal sealed record ToolCliFrameworkReferenceInspection(
    string FrameworkName,
    IReadOnlyList<string> ReferencingAssemblyPaths);
