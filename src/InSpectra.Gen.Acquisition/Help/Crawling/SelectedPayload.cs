namespace InSpectra.Gen.Acquisition.Help.Crawling;

using InSpectra.Gen.Acquisition.Help.Documents;


internal sealed record SelectedPayload(
    Document? Document,
    string? Payload,
    bool IsTerminalNonHelp,
    string? GuardrailFailureMessage = null);
