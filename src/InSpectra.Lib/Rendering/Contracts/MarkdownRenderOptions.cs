namespace InSpectra.Lib.Rendering.Contracts;

public sealed record MarkdownRenderOptions(
    int HybridSplitDepth,
    string? Title = null,
    string? CommandPrefix = null);
