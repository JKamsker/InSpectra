namespace InSpectra.Gen.Engine.Contracts.CrawlResults;

using InSpectra.Gen.Engine.Contracts.Documents;


internal sealed record SelectedPayload(
    Document? Document,
    string? Payload,
    bool IsTerminalNonHelp,
    string? GuardrailFailureMessage = null);
