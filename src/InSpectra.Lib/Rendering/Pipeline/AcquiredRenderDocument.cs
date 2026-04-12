using InSpectra.Lib.Contracts;
using InSpectra.Lib.OpenCli.Model;
using InSpectra.Lib.Rendering.Contracts;
using InSpectra.Lib.Rendering.Pipeline.Model;

namespace InSpectra.Lib.Rendering.Pipeline;

internal sealed class AcquiredRenderDocument
{
    public required OpenCliDocument RawDocument { get; init; }

    public required OpenCliDocument RenderDocument { get; init; }

    public string? XmlDocument { get; init; }

    public required RenderSourceInfo Source { get; init; }

    public OpenCliAcquisitionMetadata? Acquisition { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }
}
