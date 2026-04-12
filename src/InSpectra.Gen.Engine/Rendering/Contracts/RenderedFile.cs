namespace InSpectra.Gen.Engine.Rendering.Contracts;

public sealed record RenderedFile(
    string RelativePath,
    string FullPath,
    string? Content);
