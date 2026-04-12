namespace InSpectra.Gen.Engine.Modes.Help.Crawling;

using InSpectra.Gen.Engine.Contracts.Documents;
using InSpectra.Gen.Engine.Modes.Help.Projection;
using InSpectra.Gen.Engine.Contracts.Signatures;
using InSpectra.Gen.Engine.Tooling.DocumentPipeline.Documents;

internal static class CapturePayloadDispatcherEchoSupport
{
    public static bool ShouldRejectNonRootDispatcherEcho(
        string rootCommandName,
        string storedCommand,
        IReadOnlyList<string> commandSegments,
        Document document)
    {
        if (string.IsNullOrWhiteSpace(storedCommand)
            || commandSegments.Count == 0
            || document.Commands.Count == 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(document.Title)
            && document.Title.Trim().EndsWith(":", StringComparison.Ordinal))
        {
            return true;
        }

        if (HasLeafSurface(document))
        {
            return false;
        }

        var parentKey = string.Join(' ', commandSegments);
        if (LooksLikeSiblingUsageEcho(rootCommandName, parentKey, document.UsageLines))
        {
            return true;
        }

        var hasDescendantChild = false;
        foreach (var child in document.Commands)
        {
            if (SignatureNormalizer.IsBuiltinAuxiliaryCommand(child.Key))
            {
                continue;
            }

            var childKey = CommandPathSupport.ResolveChildKey(rootCommandName, parentKey, child.Key);
            if (childKey.Length > parentKey.Length
                && childKey.StartsWith(parentKey + " ", StringComparison.OrdinalIgnoreCase))
            {
                hasDescendantChild = true;
            }
        }

        return !hasDescendantChild;
    }

    private static bool HasLeafSurface(Document document)
        => document.Options.Count > 0
            || document.Arguments.Count > 0
            || !string.IsNullOrWhiteSpace(document.CommandDescription);

    private static bool LooksLikeSiblingUsageEcho(
        string rootCommandName,
        string parentKey,
        IReadOnlyList<string> usageLines)
    {
        if (string.IsNullOrWhiteSpace(parentKey) || usageLines.Count == 0)
        {
            return false;
        }

        var rootSegments = CommandPathSupport.SplitSegments(rootCommandName);
        foreach (var usageLine in usageLines)
        {
            var usageCommandKey = ExtractUsageCommandKey(rootSegments, usageLine);
            if (string.IsNullOrWhiteSpace(usageCommandKey))
            {
                continue;
            }

            if (string.Equals(usageCommandKey, parentKey, StringComparison.OrdinalIgnoreCase)
                || usageCommandKey.StartsWith(parentKey + " ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static string? ExtractUsageCommandKey(IReadOnlyList<string> rootSegments, string usageLine)
    {
        var tokens = usageLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return null;
        }

        var rootStart = FindTokenSequence(tokens, rootSegments);
        if (rootStart < 0)
        {
            return null;
        }

        var commandTokens = new List<string>();
        for (var index = rootStart + rootSegments.Count; index < tokens.Length; index++)
        {
            var token = tokens[index].Trim();
            if (token.Length == 0
                || token.StartsWith("<", StringComparison.Ordinal)
                || token.StartsWith("[", StringComparison.Ordinal)
                || token.StartsWith("(", StringComparison.Ordinal)
                || token.StartsWith("-", StringComparison.Ordinal)
                || token.StartsWith("/", StringComparison.Ordinal))
            {
                break;
            }

            commandTokens.Add(token);
        }

        if (commandTokens.Count == 0)
        {
            return null;
        }

        var normalized = SignatureNormalizer.NormalizeCommandKey(string.Join(' ', commandTokens));
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int FindTokenSequence(IReadOnlyList<string> tokens, IReadOnlyList<string> sequence)
    {
        if (sequence.Count == 0 || tokens.Count < sequence.Count)
        {
            return -1;
        }

        for (var start = 0; start <= tokens.Count - sequence.Count; start++)
        {
            var matched = true;
            for (var index = 0; index < sequence.Count; index++)
            {
                if (!string.Equals(tokens[start + index], sequence[index], StringComparison.OrdinalIgnoreCase))
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return start;
            }
        }

        return -1;
    }
}
