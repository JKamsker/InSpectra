namespace InSpectra.Lib.Modes.Help.Crawling;

using InSpectra.Lib.Contracts.Documents;

internal static class DocumentFingerprintSupport
{
    public static string ComputeFingerprint(Document document)
    {
        var parts = new List<string>();

        foreach (var command in document.Commands.OrderBy(c => c.Key, StringComparer.OrdinalIgnoreCase))
        {
            parts.Add($"cmd:{command.Key}");
        }

        foreach (var option in document.Options.OrderBy(o => o.Key, StringComparer.OrdinalIgnoreCase))
        {
            parts.Add($"opt:{option.Key}");
        }

        foreach (var argument in document.Arguments.OrderBy(a => a.Key, StringComparer.OrdinalIgnoreCase))
        {
            parts.Add($"arg:{argument.Key}");
        }

        return string.Join('\n', parts);
    }

    public static bool IsSignificantFingerprint(string fingerprint)
        => fingerprint.Length > 0;

    public static bool IsEchoedDocument(
        Document document,
        string commandKey,
        Dictionary<string, string> documentFingerprints)
    {
        var fingerprint = ComputeFingerprint(document);
        if (!IsSignificantFingerprint(fingerprint))
        {
            return false;
        }

        if (documentFingerprints.TryGetValue(fingerprint, out var previousKey))
        {
            return !string.Equals(previousKey, commandKey, StringComparison.OrdinalIgnoreCase);
        }

        documentFingerprints[fingerprint] = commandKey;
        return false;
    }
}
