namespace InSpectra.Lib.Tooling.Packages;

internal sealed record ToolCliFrameworkReferenceInspection(
    string FrameworkName,
    IReadOnlyList<string> ReferencingAssemblyPaths);
