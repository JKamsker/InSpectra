using InSpectra.Gen.Engine.Contracts;
using InSpectra.Gen.Engine.OpenCli.Model;
using InSpectra.Gen.Engine.Rendering.Contracts;
using InSpectra.Gen.Engine.Rendering.Pipeline.Model;

namespace InSpectra.Gen.Engine.Rendering.Pipeline;

internal sealed class AcquiredRenderDocument
{
    public required OpenCliDocument RawDocument { get; init; }

    public required OpenCliDocument RenderDocument { get; init; }

    public string? XmlDocument { get; init; }

    public required RenderSourceInfo Source { get; init; }

    public OpenCliAcquisitionMetadata? Acquisition { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }
}
