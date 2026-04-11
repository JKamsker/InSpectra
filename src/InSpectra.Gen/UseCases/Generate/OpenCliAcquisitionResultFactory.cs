using InSpectra.Gen.UseCases.Generate.Requests;
using InSpectra.Gen.Rendering.Contracts;

namespace InSpectra.Gen.UseCases.Generate;

internal static class OpenCliAcquisitionResultFactory
{
    public static OpenCliAcquisitionResult Create(
        AcquisitionResultContext context,
        string selectedMode,
        string openCliJson,
        string? xmlDocument,
        string? crawlJson,
        string? resolvedCliFramework,
        IReadOnlyList<OpenCliAcquisitionAttempt> attempts,
        IReadOnlyList<string> warnings)
    {
        var allWarnings = warnings.ToList();
        if (!string.IsNullOrWhiteSpace(context.Artifacts.CrawlOutputPath) && string.IsNullOrWhiteSpace(crawlJson))
        {
            allWarnings.Add("`--crawl-out` was requested, but the selected acquisition mode did not produce crawl data.");
        }

        var writtenArtifacts = OpenCliArtifactWriter.WriteArtifacts(context.Artifacts, openCliJson, crawlJson);
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
