namespace InSpectra.Gen.Rendering.Contracts;

public sealed record RenderedFile(
    string RelativePath,
    string FullPath,
    string? Content);
