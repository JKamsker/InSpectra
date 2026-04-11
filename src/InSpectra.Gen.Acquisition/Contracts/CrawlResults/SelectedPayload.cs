namespace InSpectra.Gen.Acquisition.Contracts.CrawlResults;

using InSpectra.Gen.Acquisition.Contracts.Documents;


internal sealed record SelectedPayload(
    Document? Document,
    string? Payload,
    bool IsTerminalNonHelp,
    string? GuardrailFailureMessage = null);
