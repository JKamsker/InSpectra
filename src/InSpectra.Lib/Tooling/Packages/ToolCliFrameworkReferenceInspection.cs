namespace InSpectra.Lib.Tooling.Packages;

public sealed record ToolCliFrameworkReferenceInspection(
    string FrameworkName,
    IReadOnlyList<string> ReferencingAssemblyPaths);
