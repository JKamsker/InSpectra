using InSpectra.Gen.Rendering.Contracts;

namespace InSpectra.Gen.UseCases.Generate.Requests;

public sealed record OpenCliAcquisitionResult(
    string OpenCliJson,
    string? XmlDocument,
    string? CrawlJson,
    RenderSourceInfo Source,
    OpenCliAcquisitionMetadata Metadata,
    IReadOnlyList<string> Warnings);
