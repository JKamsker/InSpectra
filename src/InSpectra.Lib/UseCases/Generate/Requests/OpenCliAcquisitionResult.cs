using InSpectra.Lib.Contracts;
using InSpectra.Lib.Rendering.Contracts;

namespace InSpectra.Lib.UseCases.Generate.Requests;

internal sealed record OpenCliAcquisitionResult(
    string OpenCliJson,
    string? XmlDocument,
    string? CrawlJson,
    RenderSourceInfo Source,
    OpenCliAcquisitionMetadata Metadata,
    IReadOnlyList<string> Warnings);
