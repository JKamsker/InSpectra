using InSpectra.Gen.Acquisition.Contracts.Exceptions;
using InSpectra.Gen.UseCases.Generate.Requests;
using InSpectra.Gen.Rendering.Contracts;

namespace InSpectra.Gen.UseCases.Generate;

public sealed class OpenCliGenerationService(
    IOpenCliAcquisitionService acquisitionService,
    OpenCliDocumentLoader documentLoader,
    OpenCliXmlEnricher xmlEnricher,
    OpenCliDocumentSerializer documentSerializer)
    : IOpenCliGenerationService
{
    public Task<GenerateExecutionResult> GenerateFromExecAsync(ExecAcquisitionRequest request, string? outputFile, bool overwrite, CancellationToken cancellationToken)
        => GenerateAsync(
            () => acquisitionService.AcquireFromExecAsync(request, cancellationToken),
            outputFile,
            request.Options.Artifacts,
            overwrite,
            cancellationToken);

    public Task<GenerateExecutionResult> GenerateFromDotnetAsync(DotnetAcquisitionRequest request, string? outputFile, bool overwrite, CancellationToken cancellationToken)
        => GenerateAsync(
            () => acquisitionService.AcquireFromDotnetAsync(request, cancellationToken),
            outputFile,
            request.Options.Artifacts,
            overwrite,
            cancellationToken);

    public Task<GenerateExecutionResult> GenerateFromPackageAsync(PackageAcquisitionRequest request, string? outputFile, bool overwrite, CancellationToken cancellationToken)
        => GenerateAsync(
            () => acquisitionService.AcquireFromPackageAsync(request, cancellationToken),
            outputFile,
            request.Options.Artifacts,
            overwrite,
            cancellationToken);

    private async Task<GenerateExecutionResult> GenerateAsync(
        Func<Task<OpenCliAcquisitionResult>> action,
        string? outputFile,
        OpenCliArtifactOptions artifacts,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        EnsureDistinctArtifactPaths(outputFile, artifacts.CrawlOutputPath);
        var acquisition = await action();
        var openCliJson = PrepareOpenCliJson(acquisition, out var warnings);

        string? resolvedOutputFile = null;
        if (!string.IsNullOrWhiteSpace(outputFile))
        {
            resolvedOutputFile = Path.GetFullPath(outputFile);
            OutputPathHelper.EnsureFileWritable(resolvedOutputFile, overwrite);
            var directory = Path.GetDirectoryName(resolvedOutputFile);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(resolvedOutputFile, openCliJson, cancellationToken);
        }

        return new GenerateExecutionResult(
            acquisition.Source,
            acquisition.Metadata,
            warnings,
            openCliJson,
            resolvedOutputFile);
    }

    private static void EnsureDistinctArtifactPaths(string? outputFile, string? crawlOutputPath)
    {
        if (string.IsNullOrWhiteSpace(outputFile) || string.IsNullOrWhiteSpace(crawlOutputPath))
        {
            return;
        }

        var resolvedOutputFile = Path.GetFullPath(outputFile);
        var resolvedCrawlPath = Path.GetFullPath(crawlOutputPath);
        if (!string.Equals(resolvedOutputFile, resolvedCrawlPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new CliUsageException("`--out` and `--crawl-out` must point to different files.");
    }

    private string PrepareOpenCliJson(OpenCliAcquisitionResult acquisition, out IReadOnlyList<string> warnings)
    {
        var document = documentLoader.LoadFromJson(acquisition.OpenCliJson, acquisition.Source.OpenCliOrigin);
        var collectedWarnings = acquisition.Warnings.ToList();

        if (!string.IsNullOrWhiteSpace(acquisition.XmlDocument))
        {
            var enrichment = xmlEnricher.EnrichFromXml(
                document,
                acquisition.XmlDocument,
                acquisition.Source.XmlDocOrigin ?? acquisition.Source.OpenCliOrigin);
            collectedWarnings.AddRange(enrichment.Warnings);
        }

        warnings = collectedWarnings;
        return documentSerializer.Serialize(document);
    }
}
