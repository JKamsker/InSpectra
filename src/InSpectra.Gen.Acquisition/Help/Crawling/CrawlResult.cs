namespace InSpectra.Gen.Acquisition.Help.Crawling;

using InSpectra.Gen.Acquisition.Help.Documents;


using System.Text.Json.Nodes;

internal sealed record CrawlResult(
    IReadOnlyDictionary<string, Document> Documents,
    IReadOnlyDictionary<string, JsonObject> Captures,
    IReadOnlyDictionary<string, CaptureSummary> CaptureSummaries,
    string? GuardrailFailureMessage = null);
