namespace InSpectra.Gen.Acquisition.Contracts.CrawlResults;

using InSpectra.Gen.Acquisition.Contracts.Documents;


using System.Text.Json.Nodes;

internal sealed record CrawlResult(
    IReadOnlyDictionary<string, Document> Documents,
    IReadOnlyDictionary<string, JsonObject> Captures,
    IReadOnlyDictionary<string, CaptureSummary> CaptureSummaries,
    string? GuardrailFailureMessage = null);
