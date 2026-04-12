namespace InSpectra.Gen.Acquisition.Modes.CliFx.Crawling;

using InSpectra.Gen.Acquisition.Contracts.Crawling;
using InSpectra.Gen.Acquisition.Modes.CliFx.Metadata;

using System.Text.Json.Nodes;

internal sealed record CliFxCrawlResult(
    IReadOnlyDictionary<string, CliFxHelpDocument> Documents,
    IReadOnlyDictionary<string, JsonObject> Captures,
    IReadOnlyDictionary<string, CliFxCaptureSummary> CaptureSummaries,
    string? GuardrailFailureMessage = null);
