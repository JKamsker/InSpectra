namespace InSpectra.Lib.Modes.Help.Crawling;

using InSpectra.Lib.Contracts.Crawling;

using InSpectra.Lib.Contracts.CrawlResults;

using InSpectra.Lib.Modes.Help.Projection;

using InSpectra.Lib.Contracts.Documents;
using InSpectra.Lib.Contracts.Signatures;

using InSpectra.Lib.Tooling.DocumentPipeline.Documents;
using InSpectra.Lib.Tooling.Process;

using InSpectra.Lib.Modes.Help.Parsing;

using System.Text.Json.Nodes;

internal static class CapturePayloadSupport
{
    public static SelectedCapture? SelectBestDocument(
        TextParser parser,
        string rootCommandName,
        JsonObject capture)
    {
        var storedCommand = capture["command"]?.GetValue<string>() ?? string.Empty;
        var helpInvocation = capture["helpInvocation"]?.GetValue<string>();
        SelectedCapture? bestCandidate = null;
        var bestScore = int.MinValue;

        foreach (var payload in CapturePayloadCandidateSupport.EnumeratePayloadCandidates(capture))
        {
            if (!HelpCrawlGuardrailSupport.TryValidatePayload(payload, out _))
            {
                continue;
            }

            var document = parser.Parse(payload);
            if (DocumentInspector.LooksLikeTerminalNonHelpPayload(payload))
            {
                continue;
            }

            var commandSegments = CommandPathSupport.ResolveStoredCaptureSegments(rootCommandName, storedCommand, document);
            if (!document.HasContent || !DocumentInspector.IsCompatible(commandSegments, document))
            {
                continue;
            }

            if (CapturePayloadDispatcherEchoSupport.ShouldRejectNonRootDispatcherEcho(
                    rootCommandName,
                    storedCommand,
                    commandSegments,
                    document))
            {
                continue;
            }

            var score = CapturePayloadCandidateSupport.ScorePayloadCandidate(storedCommand, document, helpInvocation, payload);
            if (score <= bestScore)
            {
                continue;
            }

            var commandKey = commandSegments.Length == 0 ? string.Empty : string.Join(' ', commandSegments);
            bestCandidate = new SelectedCapture(commandKey, document);
            bestScore = score;
        }

        return bestCandidate;
    }

    public static SelectedPayload SelectBestProcessCapture(
        TextParser parser,
        string rootCommandName,
        IReadOnlyList<string> commandSegments,
        IReadOnlyList<string> invokedArguments,
        CommandRuntime.ProcessResult processResult)
    {
        var storedCommand = commandSegments.Count == 0
            ? string.Empty
            : string.Join(' ', commandSegments);
        var helpInvocation = invokedArguments.Count == 0
            ? null
            : string.Join(' ', invokedArguments);
        var candidates = CapturePayloadCandidateSupport.EnumeratePayloadCandidates(processResult);
        Document? bestDocument = null;
        var bestPayload = candidates.FirstOrDefault();
        var bestScore = -1;
        string? guardrailFailureMessage = null;

        foreach (var payload in candidates)
        {
            if (!HelpCrawlGuardrailSupport.TryValidatePayload(payload, out var payloadFailureMessage))
            {
                guardrailFailureMessage ??= payloadFailureMessage;
                continue;
            }

            var document = parser.Parse(payload);
            var looksLikeTerminalNonHelpPayload = DocumentInspector.LooksLikeTerminalNonHelpPayload(payload);
            var compatibleDocument = !looksLikeTerminalNonHelpPayload
                && document.HasContent
                && DocumentInspector.IsCompatible(commandSegments, document)
                ? document
                : null;
            if (compatibleDocument is not null
                && CapturePayloadDispatcherEchoSupport.ShouldRejectNonRootDispatcherEcho(
                    rootCommandName,
                    storedCommand,
                    commandSegments,
                    compatibleDocument))
            {
                compatibleDocument = null;
            }

            var score = compatibleDocument is not null
                ? DocumentInspector.Score(compatibleDocument)
                    - CapturePayloadCandidateSupport.GetPayloadSelectionPenalty(payload, helpInvocation)
                : 0;
            if (score <= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestDocument = compatibleDocument;
            bestPayload = payload;
        }

        var isTerminalNonHelp = bestDocument is null && candidates.Any(DocumentInspector.LooksLikeTerminalNonHelpPayload);
        return new(bestDocument, bestPayload, isTerminalNonHelp, guardrailFailureMessage);
    }
}
