using InSpectra.Gen.Runtime.Acquisition;
using InSpectra.Gen.Runtime.Rendering;

namespace InSpectra.Gen.OpenCli.Acquisition;

internal static class OpenCliAcquisitionResultFactory
{
    public static OpenCliAcquisitionResult Create(
        string kind,
        string sourceLabel,
        string? executablePath,
        string selectedMode,
        string? commandName,
        string? cliFramework,
        string openCliJson,
        string? xmlDocument,
        string? crawlJson,
        OpenCliArtifactOptions requestedArtifacts,
        IReadOnlyList<OpenCliAcquisitionAttempt> attempts,
        IReadOnlyList<string> warnings)
    {
        var allWarnings = warnings.ToList();
        if (!string.IsNullOrWhiteSpace(requestedArtifacts.CrawlOutputPath) && string.IsNullOrWhiteSpace(crawlJson))
        {
            allWarnings.Add("`--crawl-out` was requested, but the selected acquisition mode did not produce crawl data.");
        }

        var writtenArtifacts = OpenCliArtifactWriter.WriteArtifacts(requestedArtifacts, openCliJson, crawlJson);
        return new OpenCliAcquisitionResult(
            openCliJson,
            xmlDocument,
            crawlJson,
            new RenderSourceInfo(kind, sourceLabel, xmlDocument is null ? null : sourceLabel, executablePath),
            new OpenCliAcquisitionMetadata(
                selectedMode,
                commandName,
                cliFramework,
                attempts,
                writtenArtifacts.OpenCliOutputPath,
                writtenArtifacts.CrawlOutputPath),
            allWarnings);
    }
}
