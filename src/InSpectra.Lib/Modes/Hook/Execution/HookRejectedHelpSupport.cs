namespace InSpectra.Lib.Modes.Hook.Execution;

using InSpectra.Lib.Contracts.TextClassification;
using InSpectra.Lib.Tooling.Process;

internal static class HookRejectedHelpSupport
{
    public static bool LooksLikeRejectedHelpInvocation(CommandRuntime.ProcessResult processResult)
    {
        var lines = SplitLines(processResult.Stderr)
            .Concat(SplitLines(processResult.Stdout))
            .ToArray();
        return LooksLikeRejectedHelpLines(lines);
    }

    public static bool LooksLikeRejectedHelpMessage(string? message)
        => LooksLikeRejectedHelpLines(SplitLines(message).ToArray());

    private static bool LooksLikeRejectedHelpLines(IReadOnlyList<string?> lines)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            var firstLine = NormalizeRejectedHelpLine(lines[index]);
            var secondLine = index + 1 < lines.Count
                ? NormalizeRejectedHelpLine(lines[index + 1])
                : null;
            if (RejectedHelpClassifier.LooksLikeRejectedHelpInvocation(firstLine, secondLine))
            {
                return true;
            }
        }

        return false;
    }

    private static string? NormalizeRejectedHelpLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return line;
        }

        var trimmed = line.Trim();
        var separatorIndex = trimmed.IndexOf(": ", StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex + 2 >= trimmed.Length)
        {
            return trimmed;
        }

        var prefix = trimmed[..separatorIndex];
        if (!prefix.EndsWith("Exception", StringComparison.OrdinalIgnoreCase)
            && !prefix.EndsWith("Error", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return trimmed[(separatorIndex + 2)..].TrimStart();
    }

    private static IEnumerable<string?> SplitLines(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
}
