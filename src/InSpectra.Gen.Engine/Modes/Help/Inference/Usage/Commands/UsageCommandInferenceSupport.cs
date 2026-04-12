namespace InSpectra.Gen.Engine.Modes.Help.Inference.Usage.Commands;

using InSpectra.Gen.Engine.Contracts.Documents;
using InSpectra.Gen.Engine.Modes.Help.Inference.Usage.Prototypes;
using InSpectra.Gen.Engine.Contracts.Signatures;

internal static class UsageCommandInferenceSupport
{
    public static bool LooksLikeCommandHub(
        string rootCommandName,
        IReadOnlyList<string> usageLines)
    {
        if (usageLines.Count == 0)
        {
            return false;
        }

        var rootSegments = SplitSegments(rootCommandName);
        foreach (var usageLine in usageLines)
        {
            var prototype = UsagePrototypeParsingSupport.StripLinePrefix(usageLine);
            if (UsageCommandPatternSupport.ContainsCommandPlaceholder(prototype))
            {
                return true;
            }

            var tokens = UsagePrototypeParsingSupport.TokenizeUsageLine(prototype);
            if (tokens.Length == 0 || rootSegments.Length == 0)
            {
                continue;
            }

            var rootStart = UsagePrototypeParsingSupport.FindTokenSequence(tokens, rootSegments);
            if (rootStart < 0)
            {
                continue;
            }

            var nextTokenIndex = rootStart + rootSegments.Length;
            if (nextTokenIndex < tokens.Length
                && UsageCommandPatternSupport.LooksLikeCommandPlaceholder(tokens[nextTokenIndex]))
            {
                return true;
            }
        }

        return HasRootScopedInferredCommands(rootSegments, usageLines);
    }

    public static IReadOnlyList<Item> InferCommands(IReadOnlyList<string> usageLines)
    {
        var rootSegments = UsageCommandPatternSupport.InferCommonRootSegments(usageLines);
        if (rootSegments.Length == 0)
        {
            return [];
        }

        var inferredCommands = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
        foreach (var usageLine in usageLines)
        {
            if (!UsageCommandPatternSupport.LooksLikeCommandPrototypeLine(usageLine)
                || !UsagePrototypeParsingSupport.TrySplitPrototypeAndDescription(usageLine, out var prototype, out var description))
            {
                continue;
            }

            var tokens = UsagePrototypeParsingSupport.TokenizeUsageLine(prototype);
            var rootStart = UsagePrototypeParsingSupport.FindTokenSequence(tokens, rootSegments);
            if (tokens.Length == 0 || rootStart < 0)
            {
                continue;
            }

            var commandTokens = new List<string>();
            for (var index = rootStart + rootSegments.Length; index < tokens.Length; index++)
            {
                if (!UsagePrototypeParsingSupport.LooksLikeLiteralCommandToken(tokens[index]))
                {
                    break;
                }

                commandTokens.Add(tokens[index]);
            }

            if (commandTokens.Count == 0)
            {
                continue;
            }

            var key = SignatureNormalizer.NormalizeCommandKey(string.Join(' ', commandTokens));
            if (string.IsNullOrWhiteSpace(key)
                || SignatureNormalizer.IsBuiltinAuxiliaryCommand(key))
            {
                continue;
            }

            var item = new Item(key, false, UsagePrototypeParsingSupport.NormalizeDescription(description));
            if (!inferredCommands.TryGetValue(key, out var existing)
                || UsageCommandPatternSupport.ShouldPrefer(item, existing))
            {
                inferredCommands[key] = item;
            }
        }

        return inferredCommands.Values.ToArray();
    }

    public static IReadOnlyList<string> InferChildCommands(
        string rootCommandName,
        IReadOnlyList<string> commandSegments,
        IReadOnlyList<string> usageLines)
    {
        var inferredCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rootSegments = SplitSegments(rootCommandName);

        foreach (var usageLine in usageLines)
        {
            if (!UsageCommandPatternSupport.LooksLikeCommandPrototypeLine(usageLine)
                || !UsagePrototypeParsingSupport.TrySplitPrototypeAndDescription(usageLine, out var prototype, out _))
            {
                continue;
            }

            var tokens = UsagePrototypeParsingSupport.TokenizeUsageLine(prototype);
            var rootStart = UsagePrototypeParsingSupport.FindTokenSequence(tokens, rootSegments);
            if (tokens.Length == 0 || rootStart < 0)
            {
                continue;
            }

            var candidateStart = rootStart + rootSegments.Length;
            if (!UsagePrototypeParsingSupport.StartsWith(tokens, candidateStart, commandSegments))
            {
                continue;
            }

            var nextTokenIndex = candidateStart + commandSegments.Count;
            if (nextTokenIndex >= tokens.Length
                || !UsagePrototypeParsingSupport.LooksLikeLiteralCommandToken(tokens[nextTokenIndex]))
            {
                continue;
            }

            var childKey = commandSegments.Count == 0
                ? SignatureNormalizer.NormalizeCommandKey(tokens[nextTokenIndex])
                : $"{string.Join(' ', commandSegments)} {SignatureNormalizer.NormalizeCommandKey(tokens[nextTokenIndex])}";
            if (!string.IsNullOrWhiteSpace(childKey))
            {
                inferredCommands.Add(childKey);
            }
        }

        return inferredCommands.ToArray();
    }

    private static string[] SplitSegments(string commandKey)
        => string.IsNullOrWhiteSpace(commandKey)
            ? []
            : commandKey.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool HasRootScopedInferredCommands(IReadOnlyList<string> rootSegments, IReadOnlyList<string> usageLines)
    {
        if (rootSegments.Count == 0)
        {
            return InferCommands(usageLines).Count > 0;
        }

        foreach (var usageLine in usageLines)
        {
            if (!UsageCommandPatternSupport.LooksLikeCommandPrototypeLine(usageLine)
                || !UsagePrototypeParsingSupport.TrySplitPrototypeAndDescription(usageLine, out var prototype, out _))
            {
                continue;
            }

            var tokens = UsagePrototypeParsingSupport.TokenizeUsageLine(prototype);
            var rootStart = UsagePrototypeParsingSupport.FindTokenSequence(tokens, rootSegments);
            if (tokens.Length == 0 || rootStart < 0)
            {
                continue;
            }

            var nextTokenIndex = rootStart + rootSegments.Count;
            if (nextTokenIndex < tokens.Length
                && UsagePrototypeParsingSupport.LooksLikeLiteralCommandToken(tokens[nextTokenIndex]))
            {
                return true;
            }
        }

        return false;
    }
}
