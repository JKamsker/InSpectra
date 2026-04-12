namespace InSpectra.Gen.Engine.Contracts.Signatures;

using System.Text.RegularExpressions;

/// <summary>
/// Parses a raw argument key (as it appears in help or usage text) into a normalized
/// <see cref="ArgumentSignature"/>. Lives in Contracts so both Help-mode projection and
/// Contracts-level signature tooling (e.g. <see cref="OptionTokenParsingSupport"/>) can
/// share a single parser implementation.
/// </summary>
internal static partial class ArgumentSignatureParser
{
    private static readonly HashSet<string> ArgumentNoiseWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "A", "AN", "AND", "DEFAULT", "ENTER", "FOR", "OF", "OPTIONAL", "OR", "PRESS", "THE", "TO", "USE",
    };

    public static bool TryParse(string rawKey, out ArgumentSignature signature)
    {
        signature = new ArgumentSignature(string.Empty, false);
        var trimmed = rawKey.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || OptionSignatureSupport.LooksLikeOptionPlaceholder(trimmed))
        {
            return false;
        }

        var isSequence = trimmed.Contains("...", StringComparison.Ordinal);
        var rawTokens = trimmed
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeArgumentToken)
            .Where(token => token.Length > 0)
            .ToArray();
        if (rawTokens.Length == 0 || ArgumentNoiseWords.Contains(rawTokens[0]))
        {
            return false;
        }

        string normalizedName;
        if (TryGetCommonPlaceholderStem(rawTokens, out var commonStem))
        {
            normalizedName = commonStem;
            isSequence = true;
        }
        else if (rawTokens.Length is > 1 and <= 3 && rawTokens.All(token => !ArgumentNoiseWords.Contains(token)))
        {
            normalizedName = string.Join('_', rawTokens);
        }
        else
        {
            normalizedName = rawTokens[0];
        }

        normalizedName = OptionSignatureSupport.NormalizeArgumentName(normalizedName);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return false;
        }

        signature = new ArgumentSignature(normalizedName, isSequence);
        return true;
    }

    private static bool TryGetCommonPlaceholderStem(IReadOnlyList<string> tokens, out string stem)
    {
        stem = string.Empty;
        if (tokens.Count < 2)
        {
            return false;
        }

        var stems = tokens
            .Where(token => !string.Equals(token, "...", StringComparison.Ordinal))
            .Select(token => TrailingDigitsRegex().Replace(token, string.Empty))
            .Where(token => token.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (stems.Length != 1)
        {
            return false;
        }

        stem = stems[0];
        return true;
    }

    private static string NormalizeArgumentToken(string token)
    {
        var normalized = token.Trim()
            .Trim('[', ']', '<', '>', '(', ')', '{', '}', '.', ',', ':', ';', '"', '\'');
        normalized = normalized.Replace("...", string.Empty, StringComparison.Ordinal);
        normalized = InvalidArgumentTokenRegex().Replace(normalized, string.Empty);
        return normalized;
    }

    [GeneratedRegex(@"[^A-Za-z0-9_\-]", RegexOptions.Compiled)]
    private static partial Regex InvalidArgumentTokenRegex();

    [GeneratedRegex(@"\d+$", RegexOptions.Compiled)]
    private static partial Regex TrailingDigitsRegex();
}
