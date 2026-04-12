using InSpectra.Gen.Engine.Contracts;

namespace InSpectra.Gen.Engine.Rendering.Contracts;

public sealed class RenderExecutionResult
{
    public required DocumentFormat Format { get; init; }

    public required RenderLayout Layout { get; init; }

    public required RenderSourceInfo Source { get; init; }

    public OpenCliAcquisitionMetadata? Acquisition { get; init; }

    public required RenderStats Stats { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }

    public required bool IsDryRun { get; init; }

    public string? StdoutDocument { get; init; }

    public required IReadOnlyList<RenderedFile> Files { get; init; }
}
