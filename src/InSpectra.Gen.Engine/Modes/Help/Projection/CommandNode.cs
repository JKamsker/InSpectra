namespace InSpectra.Gen.Engine.Modes.Help.Projection;


internal sealed record CommandNode(
    string FullName,
    string DisplayName,
    string? Description)
{
    public IReadOnlyList<CommandNode> Children { get; init; } = [];
}
