namespace InSpectra.Gen.Acquisition.Tooling.DocumentPipeline.Structure;

using InSpectra.Gen.Acquisition.Tooling.DocumentPipeline.Options;

using System.Text.Json.Nodes;

internal static class OpenCliNodeInputValidationSupport
{
    private static readonly string[] OptionArrayProperties = ["acceptedValues", "aliases", "arguments", "metadata"];
    private static readonly string[] ArgumentArrayProperties = ["acceptedValues", "metadata"];

    public static bool TryValidateArguments(JsonArray? arguments, string pathPrefix, out string? reason)
    {
        reason = null;
        if (arguments is null)
        {
            return true;
        }

        for (var index = 0; index < arguments.Count; index++)
        {
            var argumentPath = $"{pathPrefix}[{index}]";
            if (arguments[index] is not JsonObject argument)
            {
                reason = $"OpenCLI artifact has a non-object entry at '{argumentPath}'.";
                return false;
            }

            if (!TryValidateArgumentNode(argument, argumentPath, out reason))
            {
                return false;
            }
        }

        return true;
    }

    public static bool TryValidateOptions(JsonArray? options, string path, out string? reason)
    {
        reason = null;
        if (options is null)
        {
            return true;
        }

        var seenTokens = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < options.Count; index++)
        {
            var optionPath = $"{path}.options[{index}]";
            if (options[index] is not JsonObject option)
            {
                reason = $"OpenCLI artifact has a non-object entry at '{optionPath}'.";
                return false;
            }

            if (!TryValidateOptionNode(option, optionPath, seenTokens, out reason))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryValidateArgumentNode(JsonObject node, string path, out string? reason)
    {
        reason = null;

        if (!OpenCliNameValidationSupport.TryRequireArgumentName(
                OpenCliValidationSupport.GetString(node["name"]),
                path,
                out reason))
        {
            return false;
        }

        foreach (var arrayProperty in ArgumentArrayProperties)
        {
            if (!OpenCliValidationSupport.TryValidateArrayProperty(node, arrayProperty, path, out reason))
            {
                return false;
            }
        }

        if (node["acceptedValues"] is JsonArray acceptedValues
            && !OpenCliValidationSupport.TryValidateStringEntries(acceptedValues, $"{path}.acceptedValues", out reason))
        {
            return false;
        }

        return true;
    }

    private static bool TryValidateOptionNode(
        JsonObject node,
        string path,
        IDictionary<string, string> seenTokens,
        out string? reason)
    {
        reason = null;

        if (!OpenCliNameValidationSupport.TryRequireOptionName(
                OpenCliValidationSupport.GetString(node["name"]),
                path,
                out reason))
        {
            return false;
        }

        foreach (var arrayProperty in OptionArrayProperties)
        {
            if (!OpenCliValidationSupport.TryValidateArrayProperty(node, arrayProperty, path, out reason))
            {
                return false;
            }
        }

        if (node["aliases"] is JsonArray aliases
            && !OpenCliValidationSupport.TryValidateStringEntries(aliases, $"{path}.aliases", out reason))
        {
            return false;
        }

        if (node["aliases"] is JsonArray optionAliases
            && !TryValidateOptionAliases(optionAliases, $"{path}.aliases", out reason))
        {
            return false;
        }

        if (node["acceptedValues"] is JsonArray acceptedValues
            && !OpenCliValidationSupport.TryValidateStringEntries(acceptedValues, $"{path}.acceptedValues", out reason))
        {
            return false;
        }

        if (!TryValidateArguments(node["arguments"] as JsonArray, $"{path}.arguments", out reason))
        {
            return false;
        }

        foreach (var token in OpenCliOptionTokenValidationSupport.EnumerateOptionTokens(node))
        {
            if (seenTokens.TryGetValue(token, out var existingPath))
            {
                reason = $"OpenCLI artifact has a duplicate option token '{token}' at '{path}' colliding with '{existingPath}'.";
                return false;
            }

            seenTokens[token] = path;
        }

        return true;
    }

    private static bool TryValidateOptionAliases(JsonArray aliases, string pathPrefix, out string? reason)
    {
        reason = null;
        for (var index = 0; index < aliases.Count; index++)
        {
            if (!OpenCliNameValidationSupport.TryRequireOptionName(
                    OpenCliValidationSupport.GetString(aliases[index]),
                    $"{pathPrefix}[{index}]",
                    out reason))
            {
                return false;
            }
        }

        return true;
    }
}
