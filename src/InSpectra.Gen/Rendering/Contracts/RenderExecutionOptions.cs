namespace InSpectra.Gen.Rendering.Contracts;

public sealed record RenderExecutionOptions(
    RenderLayout Layout,
    ResolvedOutputMode OutputMode,
    bool DryRun,
    bool Quiet,
    bool Verbose,
    bool NoColor,
    bool IncludeHidden,
    bool IncludeMetadata,
    bool Overwrite,
    bool SingleFile,
    int CompressLevel,
    string? OutputFile,
    string? OutputDirectory);
