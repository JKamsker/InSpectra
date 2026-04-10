using InSpectra.Gen.Runtime;

namespace InSpectra.Gen.Services;

public sealed class OpenCliGenerationService(
    IOpenCliAcquisitionService acquisitionService,
    OpenCliDocumentLoader documentLoader,
    OpenCliXmlEnricher xmlEnricher,
    OpenCliDocumentSerializer documentSerializer)
    : IOpenCliGenerationService
{
    public Task<GenerateExecutionResult> GenerateFromExecAsync(ExecAcquisitionRequest request, string? outputFile, CancellationToken cancellationToken)
        => GenerateAsync(() => acquisitionService.AcquireFromExecAsync(request, cancellationToken), outputFile, cancellationToken);

    public Task<GenerateExecutionResult> GenerateFromDotnetAsync(DotnetAcquisitionRequest request, string? outputFile, CancellationToken cancellationToken)
        => GenerateAsync(() => acquisitionService.AcquireFromDotnetAsync(request, cancellationToken), outputFile, cancellationToken);

    public Task<GenerateExecutionResult> GenerateFromPackageAsync(PackageAcquisitionRequest request, string? outputFile, CancellationToken cancellationToken)
        => GenerateAsync(() => acquisitionService.AcquireFromPackageAsync(request, cancellationToken), outputFile, cancellationToken);

    private async Task<GenerateExecutionResult> GenerateAsync(
        Func<Task<OpenCliAcquisitionResult>> action,
        string? outputFile,
        CancellationToken cancellationToken)
    {
        var acquisition = await action();
        var openCliJson = PrepareOpenCliJson(acquisition, out var warnings);

        string? resolvedOutputFile = null;
        if (!string.IsNullOrWhiteSpace(outputFile))
        {
            resolvedOutputFile = Path.GetFullPath(outputFile);
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
