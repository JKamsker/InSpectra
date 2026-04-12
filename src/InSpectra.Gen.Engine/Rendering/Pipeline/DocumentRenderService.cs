using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.OpenCli.Enrichment;
using InSpectra.Gen.Engine.Contracts;
using InSpectra.Gen.Engine.OpenCli.Model;
using InSpectra.Gen.Engine.OpenCli.Serialization;
using InSpectra.Gen.Engine.Rendering.Contracts;
using InSpectra.Gen.Engine.Rendering.Pipeline.Model;

namespace InSpectra.Gen.Engine.Rendering.Pipeline;

internal sealed class DocumentRenderService(
    OpenCliDocumentLoader documentLoader,
    OpenCliDocumentCloner documentCloner,
    OpenCliXmlEnricher xmlEnricher)
    : IDocumentRenderService
{
    Task<AcquiredRenderDocument> IDocumentRenderService.LoadFromFileAsync(
        FileRenderRequest request,
        CancellationToken cancellationToken)
        => LoadFromFileAsync(request, cancellationToken);

    internal async Task<AcquiredRenderDocument> LoadFromFileAsync(
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

    internal static async Task<string?> LoadXmlDocumentAsync(string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var resolvedPath = Path.GetFullPath(path);
        try
        {
            return await File.ReadAllTextAsync(resolvedPath, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested && !File.Exists(resolvedPath))
        {
            throw new CliUsageException($"XML enrichment file `{resolvedPath}` does not exist.");
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new CliUsageException($"XML enrichment file `{resolvedPath}` does not exist.");
        }
        catch (UnauthorizedAccessException) when (!File.Exists(resolvedPath))
        {
            throw new CliUsageException($"XML enrichment file `{resolvedPath}` does not exist.");
        }
    }
}
