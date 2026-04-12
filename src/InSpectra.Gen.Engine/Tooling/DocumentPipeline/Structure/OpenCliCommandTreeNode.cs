namespace InSpectra.Gen.Engine.Tooling.DocumentPipeline.Structure;


internal sealed record OpenCliCommandTreeNode(
    string FullName,
    string DisplayName,
    string? Description)
{
    public IReadOnlyList<OpenCliCommandTreeNode> Children { get; init; } = [];
}
