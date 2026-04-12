namespace InSpectra.Gen.Engine.Rendering.Contracts;

public sealed record MarkdownRenderOptions(
    int HybridSplitDepth,
    string? Title = null,
    string? CommandPrefix = null);
