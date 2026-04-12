namespace InSpectra.Gen.Engine.Tooling.DocumentPipeline.Options.Collisions;

using System.Text.Json.Nodes;

internal static class OpenCliOptionCollisionMergeSupport
{
    public static bool TryMergeSafeOptionCollision(
        OpenCliOptionCollisionEntry leftEntry,
        JsonObject rightOption,
        IReadOnlySet<string> rightTokens,
        out OpenCliOptionCollisionEntry resolvedEntry)
    {
        resolvedEntry = leftEntry;
        if (leftEntry.Tokens.Count == 0 || rightTokens.Count == 0 || !leftEntry.Tokens.Overlaps(rightTokens))
        {
            return false;
        }

        if (OpenCliOptionStandaloneAliasCollisionSupport.TryResolve(leftEntry.Option, rightOption, rightTokens, out resolvedEntry))
        {
            return true;
        }

        var leftName = leftEntry.Option["name"]?.GetValue<string>();
        var rightName = rightOption["name"]?.GetValue<string>();
        if (!string.Equals(leftName, rightName, StringComparison.Ordinal))
        {
            return false;
        }

        var leftDescription = OpenCliOptionDescriptionSupport.NormalizeDescription(leftEntry.Option["description"]?.GetValue<string>());
        var rightDescription = OpenCliOptionDescriptionSupport.NormalizeDescription(rightOption["description"]?.GetValue<string>());
        var leftInformational = OpenCliOptionInformationalCollisionSupport.IsInformationalOption(leftEntry.Option, leftDescription);
        var rightInformational = OpenCliOptionInformationalCollisionSupport.IsInformationalOption(rightOption, rightDescription);
        if (OpenCliOptionInformationalCollisionSupport.TryResolveSelfArgumentDuplicate(
                leftEntry.Option,
                rightOption,
                leftInformational,
                rightInformational,
                out resolvedEntry))
        {
            return true;
        }

        if (leftInformational
            && rightInformational
            && !OpenCliOptionDescriptionSupport.HaveEquivalentInformationalTokenSets(leftEntry.Tokens, rightTokens))
        {
            // Well-known informational names (--help, --version) can safely merge when one
            // token set is a subset of the other. This handles the common case where a hook's
            // synthetic injection produces --version (no alias) alongside the framework's own
            // --version with a -v alias — they are the same semantic option.
            // Genuinely different conventions (e.g., --help/-h vs --help/?) have disjoint
            // alias sets and remain separate.
            if (!OpenCliOptionInformationalCollisionSupport.IsWellKnownInformationalName(leftName)
                || (!leftEntry.Tokens.IsSubsetOf(rightTokens) && !rightTokens.IsSubsetOf(leftEntry.Tokens)))
            {
                return false;
            }
        }

        if (leftInformational ^ rightInformational)
        {
            if (OpenCliOptionInformationalCollisionSupport.IsWellKnownInformationalName(leftName)
                || OpenCliOptionInformationalCollisionSupport.IsWellKnownInformationalName(rightName))
            {
                return false;
            }

            if (OpenCliOptionSupport.HasArguments(leftEntry.Option) || OpenCliOptionSupport.HasArguments(rightOption))
            {
                return false;
            }
        }

        if (OpenCliOptionAlternativeValueCollisionSupport.TryResolve(
                leftEntry.Option,
                rightOption,
                leftInformational,
                rightInformational,
                ChoosePreferredOption,
                out resolvedEntry))
        {
            return true;
        }

        if (!OpenCliOptionDescriptionSupport.AreCompatibleDescriptions(
                leftDescription,
                rightDescription,
                leftInformational,
                rightInformational))
        {
            return false;
        }

        var preferred = ChoosePreferredOption(leftEntry.Option, rightOption, leftInformational, rightInformational);
        var other = ReferenceEquals(preferred, leftEntry.Option) ? rightOption : leftEntry.Option;
        var merged = OpenCliOptionSupport.MergeOptions(preferred, other);
        resolvedEntry = new OpenCliOptionCollisionEntry(merged, OpenCliOptionSupport.GetOptionTokens(merged));
        return true;
    }

    private static JsonObject ChoosePreferredOption(
        JsonObject leftOption,
        JsonObject rightOption,
        bool leftInformational,
        bool rightInformational)
    {
        if (leftInformational != rightInformational)
        {
            return leftInformational ? rightOption : leftOption;
        }

        return ScoreOption(leftOption) >= ScoreOption(rightOption) ? leftOption : rightOption;
    }

    private static int ScoreOption(JsonObject option)
    {
        var score = 0;
        var name = option["name"]?.GetValue<string>() ?? string.Empty;
        if (name.StartsWith("--", StringComparison.Ordinal) || name.StartsWith("/", StringComparison.Ordinal))
        {
            score += 2;
        }

        if (option["aliases"] is JsonArray aliases)
        {
            score += aliases.Count;
        }

        if (option["arguments"] is JsonArray arguments && arguments.Count > 0)
        {
            score += 2;
        }

        var description = OpenCliOptionDescriptionSupport.NormalizeDescription(option["description"]?.GetValue<string>());
        if (!string.IsNullOrWhiteSpace(description)
            && !OpenCliOptionDescriptionSupport.IsInformationalOptionDescription(description))
        {
            score += 2;
            score += Math.Min(6, description.Length / 24);
        }

        return score;
    }
}
