namespace InSpectra.Gen.Engine.Contracts.CrawlResults;

using InSpectra.Gen.Engine.Contracts.Documents;


using System.Text.Json.Nodes;

internal sealed record CrawlResult(
    IReadOnlyDictionary<string, Document> Documents,
    IReadOnlyDictionary<string, JsonObject> Captures,
    IReadOnlyDictionary<string, CaptureSummary> CaptureSummaries,
    string? GuardrailFailureMessage = null);
