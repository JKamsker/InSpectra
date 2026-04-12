namespace InSpectra.Gen.Engine.Rendering.Contracts;

public sealed record RenderExecutionOptions(
    RenderLayout Layout,
    bool DryRun,
    bool IncludeHidden,
    bool IncludeMetadata,
    bool Overwrite,
    bool SingleFile,
    int CompressLevel,
    string? OutputFile,
    string? OutputDirectory);
