namespace InSpectra.Gen.Engine.Modes.Help.Inference.Usage.Commands;

using InSpectra.Gen.Engine.Contracts.Documents;
using InSpectra.Gen.Engine.Modes.Help.Inference.Usage.Prototypes;

internal static class UsageCommandPatternSupport
{
    public static bool ContainsCommandPlaceholder(string prototype)
    {
        var normalized = prototype.Trim();
        return normalized.Contains("[command]", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("<command>", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("[commands]", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("<commands>", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("[subcommand]", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("<subcommand>", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("[subcommands]", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("<subcommands>", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(" [cmd]", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(" <cmd>", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(" [verb]", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(" <verb>", StringComparison.OrdinalIgnoreCase);
    }

    public static string[] InferCommonRootSegments(IReadOnlyList<string> usageLines)
    {
        var literalSequences = usageLines
            .Where(LooksLikeRootPrototypeLine)
            .Select(TryExtractLeadingLiteralTokens)
            .Where(tokens => tokens.Length > 0)
            .GroupBy(tokens => tokens[0], StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault(group => group.Count() >= 2)?
            .ToArray()
            ?? [];
        if (literalSequences.Length < 2)
        {
            return [];
        }

        return ComputeCommonPrefix(literalSequences);
    }

    public static bool LooksLikeCommandPlaceholder(string token)
    {
        var normalized = token.Trim().Trim(',', ';', ':');
        return string.Equals(normalized, "[command]", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "<command>", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "[commands]", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "<commands>", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "[subcommand]", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "<subcommand>", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "[subcommands]", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "<subcommands>", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "[cmd]", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "<cmd>", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "[verb]", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "<verb>", StringComparison.OrdinalIgnoreCase);
    }

    public static bool LooksLikeCommandPrototypeLine(string usageLine)
    {
        if (!UsagePrototypeParsingSupport.TrySplitPrototypeAndDescription(usageLine, out var prototype, out var description))
        {
            return false;
        }

        if (prototype.Length == 0)
        {
            return false;
        }

        if (UsagePrototypeParsingSupport.ContainsUsageSyntaxMarker(prototype))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(description) || LooksLikeDecorativeDescription(description))
        {
            return false;
        }

        var tokens = UsagePrototypeParsingSupport.TokenizeUsageLine(prototype);
        return tokens.Length is >= 2 and <= 4
            && tokens.All(UsagePrototypeParsingSupport.LooksLikeLiteralCommandToken);
    }

    public static bool ShouldPrefer(Item candidate, Item existing)
    {
        var candidateDescriptionLength = candidate.Description?.Length ?? 0;
        var existingDescriptionLength = existing.Description?.Length ?? 0;
        return candidateDescriptionLength > existingDescriptionLength;
    }

    private static string[] ComputeCommonPrefix(string[][] literalSequences)
    {
        var commonLength = literalSequences.Min(tokens => tokens.Length);
        for (var index = 0; index < commonLength; index++)
        {
            var expected = literalSequences[0][index];
            if (literalSequences.Any(tokens => !string.Equals(tokens[index], expected, StringComparison.OrdinalIgnoreCase)))
            {
                return index == 0 ? [] : literalSequences[0][..index];
            }
        }

        return literalSequences[0][..commonLength];
    }

    private static bool LooksLikeDecorativeDescription(string description)
        => description.All(ch => !char.IsLetterOrDigit(ch));

    private static bool LooksLikeRootPrototypeLine(string usageLine)
    {
        if (!UsagePrototypeParsingSupport.TrySplitPrototypeAndDescription(usageLine, out var prototype, out var description))
        {
            return false;
        }

        if (prototype.Length == 0)
        {
            return false;
        }

        if (UsagePrototypeParsingSupport.ContainsUsageSyntaxMarker(prototype))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(description) || LooksLikeDecorativeDescription(description))
        {
            return false;
        }

        var tokens = UsagePrototypeParsingSupport.TokenizeUsageLine(prototype);
        return tokens.Length >= 1
            && tokens.All(UsagePrototypeParsingSupport.LooksLikeLiteralCommandToken);
    }

    private static string[] TryExtractLeadingLiteralTokens(string usageLine)
    {
        if (!UsagePrototypeParsingSupport.TrySplitPrototypeAndDescription(usageLine, out var prototype, out _))
        {
            return [];
        }

        return UsagePrototypeParsingSupport.TokenizeUsageLine(prototype)
            .TakeWhile(UsagePrototypeParsingSupport.LooksLikeLiteralCommandToken)
            .ToArray();
    }
}
