namespace InSpectra.Lib.Modes.CliFx.Crawling;

using InSpectra.Lib.Contracts.Crawling;
using InSpectra.Lib.Modes.CliFx.Metadata;

using System.Text.Json.Nodes;

internal sealed record CliFxCrawlResult(
    IReadOnlyDictionary<string, CliFxHelpDocument> Documents,
    IReadOnlyDictionary<string, JsonObject> Captures,
    IReadOnlyDictionary<string, CliFxCaptureSummary> CaptureSummaries,
    string? GuardrailFailureMessage = null);
