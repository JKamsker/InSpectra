using InSpectra.Lib.Contracts;
using InSpectra.Lib.UseCases.Generate.Requests;
using InSpectra.Lib.Rendering.Contracts;

namespace InSpectra.Lib.UseCases.Generate;

internal static class OpenCliAcquisitionResultFactory
{
    public static async Task<OpenCliAcquisitionResult> CreateAsync(
        AcquisitionResultContext context,
        string selectedMode,
        string openCliJson,
        string? xmlDocument,
        string? crawlJson,
        string? resolvedCliFramework,
        IReadOnlyList<OpenCliAcquisitionAttempt> attempts,
        IReadOnlyList<string> warnings,
        CancellationToken cancellationToken)
    {
        var allWarnings = warnings.ToList();
        if (!string.IsNullOrWhiteSpace(context.Artifacts.CrawlOutputPath) && string.IsNullOrWhiteSpace(crawlJson))
        {
            allWarnings.Add("Crawl artifact output was requested, but the selected acquisition mode did not produce crawl data.");
        }

        var writtenArtifacts = await OpenCliArtifactWriter.WriteArtifactsAsync(
            context.Artifacts,
            openCliJson,
            crawlJson,
            cancellationToken);
        return new OpenCliAcquisitionResult(
            openCliJson,
            xmlDocument,
            crawlJson,
            new RenderSourceInfo(context.Kind, context.SourceLabel, xmlDocument is null ? null : context.SourceLabel, context.ReportedExecutablePath),
            new OpenCliAcquisitionMetadata(
                selectedMode,
                context.CommandName,
                resolvedCliFramework,
                attempts,
                writtenArtifacts.OpenCliOutputPath,
                writtenArtifacts.CrawlOutputPath),
            allWarnings);
    }
}
