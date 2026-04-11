namespace InSpectra.Gen.Acquisition.Tooling.DocumentPipeline.Options.Collisions;

using System.Text.Json.Nodes;

internal static class OpenCliOptionAlternativeValueCollisionSupport
{
    public static bool TryResolve(
        JsonObject leftOption,
        JsonObject rightOption,
        bool leftInformational,
        bool rightInformational,
        Func<JsonObject, JsonObject, bool, bool, JsonObject> choosePreferredOption,
        out OpenCliOptionCollisionEntry resolvedEntry)
    {
        resolvedEntry = CreateEntry(leftOption);
        if (leftInformational
            || rightInformational
            || !OpenCliOptionSupport.HasArguments(leftOption)
            || !OpenCliOptionSupport.HasArguments(rightOption))
        {
            return false;
        }

        var leftTokens = OpenCliOptionSupport.GetOptionTokens(leftOption);
        var rightTokens = OpenCliOptionSupport.GetOptionTokens(rightOption);
        if (!leftTokens.SetEquals(rightTokens))
        {
            return false;
        }

        var leftArgument = GetSingleArgument(leftOption);
        var rightArgument = GetSingleArgument(rightOption);
        if (leftArgument is null || rightArgument is null)
        {
            return false;
        }

        var leftDescription = leftOption["description"]?.GetValue<string>();
        var rightDescription = rightOption["description"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(leftDescription) || string.IsNullOrWhiteSpace(rightDescription))
        {
            return false;
        }

        var preferred = choosePreferredOption(leftOption, rightOption, leftInformational, rightInformational);
        var other = ReferenceEquals(preferred, leftOption) ? rightOption : leftOption;
        var merged = OpenCliOptionSupport.MergeOptions(preferred, other);
        merged["description"] = MergeDescriptions(leftDescription, rightDescription);
        merged["arguments"] = new JsonArray
        {
            BuildMergedArgument(leftOption["name"]?.GetValue<string>(), leftArgument, rightArgument),
        };

        resolvedEntry = CreateEntry(merged);
        return true;
    }

    private static JsonObject? GetSingleArgument(JsonObject option)
        => option["arguments"] is JsonArray arguments && arguments.Count == 1
            ? arguments[0] as JsonObject
            : null;

    private static string MergeDescriptions(string leftDescription, string rightDescription)
    {
        var lines = leftDescription
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Concat(rightDescription.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return string.Join("\n", lines);
    }

    private static JsonObject BuildMergedArgument(string? optionName, JsonObject leftArgument, JsonObject rightArgument)
    {
        var mergedArgument = (JsonObject)leftArgument.DeepClone();
        mergedArgument["name"] = OpenCliOptionSupport.DeriveSyntheticArgumentName(optionName);

        var leftRequired = leftArgument["required"]?.GetValue<bool>() == true;
        var rightRequired = rightArgument["required"]?.GetValue<bool>() == true;
        mergedArgument["required"] = leftRequired && rightRequired;

        if (mergedArgument["arity"] is JsonObject arity)
        {
            arity["minimum"] = leftRequired && rightRequired ? 1 : 0;
        }

        return mergedArgument;
    }

    private static OpenCliOptionCollisionEntry CreateEntry(JsonObject option)
        => new(option, OpenCliOptionSupport.GetOptionTokens(option));
}
