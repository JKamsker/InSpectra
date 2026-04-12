using InSpectra.Gen.Core;
using InSpectra.Gen.Rendering.Contracts;
using InSpectra.Gen.Rendering.Pipeline;
using InSpectra.Gen.UseCases.Generate.Requests;

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
            () => acquisitionService.AcquireFromExecAsync(WithoutArtifactPublication(request), cancellationToken),
            outputFile,
            request.Options.Artifacts,
            overwrite,
            cancellationToken);

    public Task<GenerateExecutionResult> GenerateFromDotnetAsync(DotnetAcquisitionRequest request, string? outputFile, bool overwrite, CancellationToken cancellationToken)
        => GenerateAsync(
            () => acquisitionService.AcquireFromDotnetAsync(WithoutArtifactPublication(request), cancellationToken),
            outputFile,
            request.Options.Artifacts,
            overwrite,
            cancellationToken);

    public Task<GenerateExecutionResult> GenerateFromPackageAsync(PackageAcquisitionRequest request, string? outputFile, bool overwrite, CancellationToken cancellationToken)
        => GenerateAsync(
            () => acquisitionService.AcquireFromPackageAsync(WithoutArtifactPublication(request), cancellationToken),
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
        var publishedArtifacts = await OpenCliArtifactWriter.WriteGenerateArtifactsAsync(
            outputFile,
            overwrite,
            artifacts,
            openCliJson,
            acquisition.OpenCliJson,
            acquisition.CrawlJson,
            cancellationToken);
        var allWarnings = warnings.ToList();
        if (!string.IsNullOrWhiteSpace(artifacts.CrawlOutputPath) && string.IsNullOrWhiteSpace(acquisition.CrawlJson))
        {
            allWarnings.Add("`--crawl-out` was requested, but the selected acquisition mode did not produce crawl data.");
        }

        return new GenerateExecutionResult(
            acquisition.Source,
            acquisition.Metadata with
            {
                OpenCliOutputPath = publishedArtifacts.OpenCliOutputPath,
                CrawlOutputPath = publishedArtifacts.CrawlOutputPath,
            },
            allWarnings,
            openCliJson,
            publishedArtifacts.OutputFile);
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

    private static ExecAcquisitionRequest WithoutArtifactPublication(ExecAcquisitionRequest request)
        => request with
        {
            Options = WithoutArtifactPublication(request.Options),
        };

    private static DotnetAcquisitionRequest WithoutArtifactPublication(DotnetAcquisitionRequest request)
        => request with
        {
            Options = WithoutArtifactPublication(request.Options),
        };

    private static PackageAcquisitionRequest WithoutArtifactPublication(PackageAcquisitionRequest request)
        => request with
        {
            Options = WithoutArtifactPublication(request.Options),
        };

    private static AcquisitionOptions WithoutArtifactPublication(AcquisitionOptions options)
        => options with
        {
            Artifacts = new OpenCliArtifactOptions(null, null, options.Artifacts.Overwrite),
        };
}
