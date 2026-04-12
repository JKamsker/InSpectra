using InSpectra.Gen.Engine.Contracts;
using InSpectra.Gen.Engine.Rendering.Contracts;

namespace InSpectra.Gen.Engine.UseCases.Generate.Requests;

internal sealed record OpenCliAcquisitionResult(
    string OpenCliJson,
    string? XmlDocument,
    string? CrawlJson,
    RenderSourceInfo Source,
    OpenCliAcquisitionMetadata Metadata,
    IReadOnlyList<string> Warnings);
