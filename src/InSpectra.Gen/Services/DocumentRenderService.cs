using InSpectra.Gen.Models;
using InSpectra.Gen.Runtime;

namespace InSpectra.Gen.Services;

public sealed class AcquiredRenderDocument
{
    public required OpenCliDocument RawDocument { get; init; }

    public required OpenCliDocument RenderDocument { get; init; }

    public string? XmlDocument { get; init; }

    public required RenderSourceInfo Source { get; init; }

    public OpenCliAcquisitionMetadata? Acquisition { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }
}

public sealed class DocumentRenderService(
    OpenCliDocumentLoader documentLoader,
    OpenCliDocumentCloner documentCloner,
    OpenCliXmlEnricher xmlEnricher)
    : IDocumentRenderService
{
    public async Task<AcquiredRenderDocument> LoadFromFileAsync(
        FileRenderRequest request,
        CancellationToken cancellationToken)
    {
        var rawDocument = await documentLoader.LoadFromFileAsync(request.OpenCliJsonPath, cancellationToken);
        var xmlDocument = await LoadXmlDocumentAsync(request.XmlDocPath, cancellationToken);

        return CreatePreparedDocument(
            rawDocument,
            xmlDocument,
            new RenderSourceInfo(
                "file",
                Path.GetFullPath(request.OpenCliJsonPath),
                request.XmlDocPath is null ? null : Path.GetFullPath(request.XmlDocPath),
                null));
    }

    private AcquiredRenderDocument CreatePreparedDocument(
        OpenCliDocument rawDocument,
        string? xmlDocument,
        RenderSourceInfo source,
        OpenCliAcquisitionMetadata? acquisition = null,
        IReadOnlyList<string>? acquisitionWarnings = null)
    {
        var renderDocument = documentCloner.Clone(rawDocument);
        var warnings = acquisitionWarnings?.ToList() ?? [];

        if (!string.IsNullOrWhiteSpace(xmlDocument))
        {
            var enrichment = xmlEnricher.EnrichFromXml(
                renderDocument,
                xmlDocument,
                source.XmlDocOrigin ?? source.OpenCliOrigin);
            warnings.AddRange(enrichment.Warnings);
        }

        return new AcquiredRenderDocument
        {
            RawDocument = rawDocument,
            RenderDocument = renderDocument,
            XmlDocument = xmlDocument,
            Source = source,
            Acquisition = acquisition,
            Warnings = warnings,
        };
    }

    private static async Task<string?> LoadXmlDocumentAsync(string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var resolvedPath = Path.GetFullPath(path);
        if (!File.Exists(resolvedPath))
        {
            throw new CliUsageException($"XML enrichment file `{resolvedPath}` does not exist.");
        }

        return await File.ReadAllTextAsync(resolvedPath, cancellationToken);
    }
}
