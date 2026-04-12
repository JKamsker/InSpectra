namespace InSpectra.Gen.Engine.Tooling.Packages;

internal sealed record ToolCliFrameworkReferenceInspection(
    string FrameworkName,
    IReadOnlyList<string> ReferencingAssemblyPaths);
