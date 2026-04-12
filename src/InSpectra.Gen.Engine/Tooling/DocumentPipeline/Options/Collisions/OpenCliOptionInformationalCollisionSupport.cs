namespace InSpectra.Gen.Engine.Tooling.DocumentPipeline.Options.Collisions;

using System.Text.Json.Nodes;

internal static class OpenCliOptionInformationalCollisionSupport
{
    public static bool IsInformationalOption(JsonObject option, string normalizedDescription)
    {
        if (OpenCliOptionDescriptionSupport.IsInformationalOptionDescription(normalizedDescription))
        {
            return true;
        }

        return OpenCliOptionDescriptionSupport.LooksLikeWellKnownInformationalOptionDescription(
                option["name"]?.GetValue<string>(),
                normalizedDescription)
            && (!OpenCliOptionSupport.HasArguments(option) || HasIgnorableArguments(option));
    }

    public static bool IsWellKnownInformationalName(string? optionName)
        => NormalizeOptionSemanticName(optionName) is "help" or "version";

    public static bool TryResolveSelfArgumentDuplicate(
        JsonObject leftOption,
        JsonObject rightOption,
        bool leftInformational,
        bool rightInformational,
        out OpenCliOptionCollisionEntry resolvedEntry)
    {
        resolvedEntry = CreateEntry(leftOption);
        if (TryResolveSelfArgumentDuplicateCore(leftOption, rightOption, leftInformational, out var merged))
        {
            resolvedEntry = CreateEntry(merged);
            return true;
        }

        if (TryResolveSelfArgumentDuplicateCore(rightOption, leftOption, rightInformational, out merged))
        {
            resolvedEntry = CreateEntry(merged);
            return true;
        }

        return false;
    }

    private static bool TryResolveSelfArgumentDuplicateCore(
        JsonObject informationalCandidate,
        JsonObject selfArgumentCandidate,
        bool isInformationalCandidate,
        out JsonObject resolved)
    {
        resolved = informationalCandidate;
        if (!isInformationalCandidate
            || OpenCliOptionSupport.HasArguments(informationalCandidate)
            || !LooksLikeSyntheticSelfArgumentOption(selfArgumentCandidate))
        {
            return false;
        }

        resolved = OpenCliOptionSupport.MergeOptions(informationalCandidate, selfArgumentCandidate);
        resolved.Remove("arguments");
        return true;
    }

    private static bool LooksLikeSyntheticSelfArgumentOption(JsonObject option)
    {
        var normalizedDescription = OpenCliOptionDescriptionSupport.NormalizeDescription(option["description"]?.GetValue<string>());
        if (!OpenCliOptionDescriptionSupport.LooksLikeWellKnownInformationalOptionDescription(
                option["name"]?.GetValue<string>(),
                normalizedDescription))
        {
            return false;
        }

        return HasIgnorableArguments(option);
    }

    private static bool HasIgnorableArguments(JsonObject option)
    {
        if (option["arguments"] is not JsonArray arguments || arguments.Count != 1)
        {
            return false;
        }

        var argument = arguments[0] as JsonObject;
        if (argument is null || argument["required"]?.GetValue<bool>() == true)
        {
            return false;
        }

        if (LooksLikeOptionalBooleanNoise(argument))
        {
            return true;
        }

        var argumentName = argument["name"]?.GetValue<string>();
        return !string.IsNullOrWhiteSpace(argumentName)
            && string.Equals(
                argumentName,
                OpenCliOptionSupport.DeriveSyntheticArgumentName(option["name"]?.GetValue<string>()),
                StringComparison.Ordinal);
    }

    private static bool LooksLikeOptionalBooleanNoise(JsonObject argument)
    {
        if (!string.IsNullOrWhiteSpace(argument["name"]?.GetValue<string>())
            || !string.Equals(argument["type"]?.GetValue<string>(), "Boolean", StringComparison.Ordinal)
            || argument["arity"] is not JsonObject arity)
        {
            return false;
        }

        var minimum = arity["minimum"]?.GetValue<int>();
        var maximum = arity["maximum"]?.GetValue<int>();
        return minimum == 0 && maximum == 1;
    }

    private static string NormalizeOptionSemanticName(string? optionName)
        => string.IsNullOrWhiteSpace(optionName)
            ? string.Empty
            : optionName.Trim().TrimStart('-', '/').ToLowerInvariant();

    private static OpenCliOptionCollisionEntry CreateEntry(JsonObject option)
        => new(option, OpenCliOptionSupport.GetOptionTokens(option));
}
