namespace InSpectra.Gen.Engine.Contracts.TextClassification;

using System.Text.RegularExpressions;

/// <summary>
/// Heuristic recognizer for stderr/stdout lines that indicate a tool refused a help
/// invocation (<c>--help</c>, <c>-h</c>, <c>-?</c>, etc.).
///
/// <para>
/// Kept in Contracts because both Help-mode text noise classification and Hook-mode
/// rejected-help detection need the same regex. Exposing it here avoids cross-mode
/// imports between Hook and Help.
/// </para>
/// </summary>
internal static partial class RejectedHelpClassifier
{
    public static bool LooksLikeRejectedHelpInvocation(string? firstLine, string? secondLine)
    {
        if (LooksLikeRejectedHelpInvocation(firstLine))
        {
            return true;
        }

        return string.Equals(firstLine?.Trim(), "ERROR(S):", StringComparison.OrdinalIgnoreCase)
            && LooksLikeRejectedHelpInvocation(secondLine);
    }

    public static bool LooksLikeRejectedHelpInvocation(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        return RejectedHelpInvocationRegex().IsMatch(line.Trim());
    }

    [GeneratedRegex(@"^(?:--help|-h|-\?|/\?)\s+is an unknown (?:parameter|option|argument)\b|^Invalid usage\b|^Invalid argument:\b|^Unknown argument or flag for value --help\b|^Unknown operation:\s+help\b|^Need to insert a value for the option\b|^(?:unknown|unrecognized)\s+(?:option|parameter|argument)\b.*(?:--help|-h|-\?|/\?|--h)\b|^(?:unknown|unrecognized)\s+command\b.*\bhelp\b|^usage error\b.*(?:--help|-h|-\?|/\?|--h)\b|^error\(\d+\):\s+unknown command-line option\s+(?:--help|-h|-\?|/\?|--h)\b|^Verb\s+'(?:--help|-h|-\?|/\?|--h)'\s+is not recognized\.$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex RejectedHelpInvocationRegex();
}
