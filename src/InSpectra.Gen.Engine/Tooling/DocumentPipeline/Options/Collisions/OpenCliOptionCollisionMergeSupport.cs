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

        // Two options sharing the same well-known informational primary name (e.g. `--version`
        // appearing twice: once as System.CommandLine's auto-registered VersionOption and once
        // as a tool-author's redefined version flag) refer to the same semantic concept. Merge
        // them by preferring the richer option so the document stays publishable. Only applies
        // when neither side carries meaningful argument metadata — options that take a value
        // argument (e.g. `--version <VERSION>` on a package publisher) are genuinely different
        // and must stay separate (see OpenCliDocumentSanitizerOptionMergeTests).
        if (OpenCliOptionInformationalCollisionSupport.IsWellKnownInformationalName(leftName)
            && OpenCliOptionInformationalCollisionSupport.IsWellKnownInformationalName(rightName)
            && !OpenCliOptionSupport.HasArguments(leftEntry.Option)
            && !OpenCliOptionSupport.HasArguments(rightOption))
        {
            var preferredWellKnown = ChoosePreferredOption(leftEntry.Option, rightOption, leftInformational, rightInformational);
            var otherWellKnown = ReferenceEquals(preferredWellKnown, leftEntry.Option) ? rightOption : leftEntry.Option;
            var mergedWellKnown = OpenCliOptionSupport.MergeOptions(preferredWellKnown, otherWellKnown);
            resolvedEntry = new OpenCliOptionCollisionEntry(mergedWellKnown, OpenCliOptionSupport.GetOptionTokens(mergedWellKnown));
            return true;
        }

        if (leftInformational
            && rightInformational
            && !OpenCliOptionDescriptionSupport.HaveEquivalentInformationalTokenSets(leftEntry.Tokens, rightTokens))
        {
            return false;
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
