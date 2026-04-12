namespace InSpectra.Lib.Contracts.CrawlResults;

using InSpectra.Lib.Contracts.Documents;


internal sealed record SelectedPayload(
    Document? Document,
    string? Payload,
    bool IsTerminalNonHelp,
    string? GuardrailFailureMessage = null);
