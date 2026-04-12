namespace InSpectra.Lib.OpenCli.Enrichment;

internal sealed class XmlEnrichmentResult
{
    public int MatchedCommandCount { get; set; }

    public int EnrichedDescriptionCount { get; set; }

    public List<string> Warnings { get; } = [];
}
