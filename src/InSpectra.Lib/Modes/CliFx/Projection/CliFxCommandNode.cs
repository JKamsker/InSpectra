namespace InSpectra.Lib.Modes.CliFx.Projection;

internal sealed record CliFxCommandNode(
    string FullName,
    string DisplayName,
    string? Description)
{
    public IReadOnlyList<CliFxCommandNode> Children { get; init; } = [];
}
