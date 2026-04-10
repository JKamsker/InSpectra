using InSpectra.Gen.Models;
using InSpectra.Gen.Runtime.Acquisition;
using InSpectra.Gen.Runtime.Rendering;

namespace InSpectra.Gen.Rendering;

public sealed class AcquiredRenderDocument
{
    public required OpenCliDocument RawDocument { get; init; }

    public required OpenCliDocument RenderDocument { get; init; }

    public string? XmlDocument { get; init; }

    public required RenderSourceInfo Source { get; init; }

    public OpenCliAcquisitionMetadata? Acquisition { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }
}
