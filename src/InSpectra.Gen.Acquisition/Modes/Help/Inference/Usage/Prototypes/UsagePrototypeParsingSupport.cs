namespace InSpectra.Gen.Acquisition.Modes.Help.Inference.Usage.Prototypes;

using InSpectra.Gen.Acquisition.Contracts.Signatures;

internal static class UsagePrototypeParsingSupport
{
    public static bool ContainsUsageSyntaxMarker(string prototype)
        => prototype.Contains('[', StringComparison.Ordinal)
            || prototype.Contains('<', StringComparison.Ordinal)
            || prototype.Contains("--", StringComparison.Ordinal)
            || prototype.Contains("...", StringComparison.Ordinal)
            || prototype.Contains('|', StringComparison.Ordinal);

    public static int FindTokenSequence(IReadOnlyList<string> tokens, IReadOnlyList<string> sequence)
    {
        if (sequence.Count == 0 || tokens.Count < sequence.Count)
        {
            return -1;
        }

        for (var start = 0; start <= tokens.Count - sequence.Count; start++)
        {
            if (StartsWith(tokens, start, sequence))
            {
                return start;
            }
        }

        return -1;
    }

    public static bool LooksLikeLiteralCommandToken(string token)
    {
        var trimmed = token.Trim().TrimEnd(':');
        if (trimmed.Length == 0
            || trimmed.StartsWith("<", StringComparison.Ordinal)
            || trimmed.StartsWith("[", StringComparison.Ordinal)
            || trimmed.StartsWith("(", StringComparison.Ordinal)
            || trimmed.StartsWith("-", StringComparison.Ordinal)
            || trimmed.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        var normalized = SignatureNormalizer.NormalizeCommandKey(trimmed);
        return !string.IsNullOrWhiteSpace(normalized)
            && string.Equals(normalized, trimmed, StringComparison.OrdinalIgnoreCase);
    }

    public static string? NormalizeDescription(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public static bool StartsWith(IReadOnlyList<string> tokens, int start, IReadOnlyList<string> sequence)
    {
        if (start < 0 || start + sequence.Count > tokens.Count)
        {
            return false;
        }

        for (var index = 0; index < sequence.Count; index++)
        {
            if (!string.Equals(tokens[start + index], sequence[index], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    public static string StripLinePrefix(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith("OR:", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed[3..].TrimStart();
        }

        if (trimmed.StartsWith("OR ", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed[2..].TrimStart();
        }

        return trimmed;
    }

    public static string[] TokenizeUsageLine(string line)
        => StripLinePrefix(line).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.Trim().Trim(',', ';'))
            .Where(token => token.Length > 0)
            .ToArray();

    public static bool TrySplitPrototypeAndDescription(string usageLine, out string prototype, out string? description)
    {
        prototype = StripLinePrefix(usageLine);
        description = null;
        if (prototype.Length == 0)
        {
            return false;
        }

        var gapIndex = prototype.IndexOf("  ", StringComparison.Ordinal);
        if (gapIndex < 0)
        {
            return true;
        }

        var separatorLength = 2;
        while (gapIndex + separatorLength < prototype.Length
            && prototype[gapIndex + separatorLength] == ' ')
        {
            separatorLength++;
        }

        var prototypeCandidate = prototype[..gapIndex].TrimEnd();
        var descriptionCandidate = prototype[(gapIndex + separatorLength)..].Trim();
        if (prototypeCandidate.Length == 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(descriptionCandidate))
        {
            prototype = prototypeCandidate;
            description = descriptionCandidate;
        }

        return true;
    }
}
