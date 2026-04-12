namespace InSpectra.Lib.Modes.Help.Inference.Usage.Prototypes;

using InSpectra.Lib.Contracts.Signatures;

internal static class UsagePrototypeSupport
{
    public static IReadOnlyList<UsagePrototype> ExtractLeafCommandPrototypes(
        string rootCommandName,
        string commandPath,
        IReadOnlyList<string> usageLines)
    {
        var rootSegments = SplitSegments(rootCommandName);
        var commandSegments = SplitSegments(commandPath);
        var pathSegments = rootSegments.Concat(commandSegments).ToArray();
        if (pathSegments.Length == 0)
        {
            return [];
        }

        var results = new List<UsagePrototype>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var usageLine in usageLines)
        {
            if (!TrySplitPrototypeAndDescription(usageLine, out var prototype, out var description))
            {
                continue;
            }

            var tokens = UsagePrototypeParsingSupport.TokenizeUsageLine(prototype);
            var pathStart = UsagePrototypeParsingSupport.FindTokenSequence(tokens, pathSegments);
            if (pathStart < 0)
            {
                continue;
            }

            var nextTokenIndex = pathStart + pathSegments.Length;
            if (nextTokenIndex < tokens.Length
                && UsagePrototypeParsingSupport.LooksLikeLiteralCommandToken(tokens[nextTokenIndex]))
            {
                continue;
            }

            var normalizedPrototype = prototype.Trim();
            if (normalizedPrototype.Length == 0 || !seen.Add(normalizedPrototype))
            {
                continue;
            }

            results.Add(new UsagePrototype(normalizedPrototype, UsagePrototypeParsingSupport.NormalizeDescription(description)));
        }

        return results;
    }

    private static string[] SplitSegments(string commandKey)
        => string.IsNullOrWhiteSpace(commandKey)
            ? []
            : commandKey.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool TrySplitPrototypeAndDescription(string usageLine, out string prototype, out string? description)
        => UsagePrototypeParsingSupport.TrySplitPrototypeAndDescription(usageLine, out prototype, out description);
}

internal sealed record UsagePrototype(
    string Prototype,
    string? Description);
