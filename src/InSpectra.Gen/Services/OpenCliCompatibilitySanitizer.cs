using System.Text.Json.Nodes;

namespace InSpectra.Gen.Services;

internal static class OpenCliCompatibilitySanitizer
{
    public static JsonNode Sanitize(JsonNode rootNode)
    {
        if (rootNode is JsonObject rootObject)
        {
            SanitizeDocument(rootObject);
        }

        return rootNode;
    }

    private static void SanitizeDocument(JsonObject document)
    {
        NormalizeObjectArrayProperty(document, "arguments", SanitizeArgument);
        NormalizeObjectArrayProperty(document, "options", SanitizeOption);
        NormalizeObjectArrayProperty(document, "commands", SanitizeCommand);
        NormalizeObjectArrayProperty(document, "exitCodes", static _ => { });
        NormalizeStringArrayProperty(document, "examples");
        NormalizeObjectArrayProperty(document, "metadata", static _ => { });
    }

    private static void SanitizeCommand(JsonObject command)
    {
        NormalizeStringArrayProperty(command, "aliases");
        NormalizeObjectArrayProperty(command, "options", SanitizeOption);
        NormalizeObjectArrayProperty(command, "arguments", SanitizeArgument);
        NormalizeObjectArrayProperty(command, "commands", SanitizeCommand);
        NormalizeObjectArrayProperty(command, "exitCodes", static _ => { });
        NormalizeStringArrayProperty(command, "examples");
        NormalizeObjectArrayProperty(command, "metadata", static _ => { });
    }

    private static void SanitizeOption(JsonObject option)
    {
        NormalizeStringArrayProperty(option, "aliases");
        NormalizeObjectArrayProperty(option, "arguments", SanitizeArgument);
        NormalizeObjectArrayProperty(option, "metadata", static _ => { });
    }

    private static void SanitizeArgument(JsonObject argument)
    {
        NormalizeStringArrayProperty(argument, "acceptedValues");
        NormalizeObjectArrayProperty(argument, "metadata", static _ => { });
    }

    private static void NormalizeObjectArrayProperty(
        JsonObject parent,
        string propertyName,
        Action<JsonObject> sanitizeObject)
    {
        if (!parent.TryGetPropertyValue(propertyName, out var value) || value is null)
        {
            parent[propertyName] = new JsonArray();
            return;
        }

        var normalized = new JsonArray();
        foreach (var item in EnumerateObjects(value))
        {
            sanitizeObject(item);
            normalized.Add(item);
        }

        parent[propertyName] = normalized;
    }

    private static IEnumerable<JsonObject> EnumerateObjects(JsonNode value)
    {
        if (value is JsonObject singleObject)
        {
            yield return (JsonObject)singleObject.DeepClone();
            yield break;
        }

        if (value is not JsonArray array)
        {
            yield break;
        }

        foreach (var item in array)
        {
            if (item is JsonObject objectItem)
            {
                yield return (JsonObject)objectItem.DeepClone();
            }
        }
    }

    private static void NormalizeStringArrayProperty(JsonObject parent, string propertyName)
    {
        if (!parent.TryGetPropertyValue(propertyName, out var value) || value is null)
        {
            parent[propertyName] = new JsonArray();
            return;
        }

        var normalized = new JsonArray();
        foreach (var item in EnumerateStrings(value))
        {
            normalized.Add(item);
        }

        parent[propertyName] = normalized;
    }

    private static IEnumerable<string> EnumerateStrings(JsonNode value)
    {
        if (TryGetString(value, out var singleValue))
        {
            yield return singleValue;
            yield break;
        }

        if (value is not JsonArray array)
        {
            yield break;
        }

        foreach (var item in array)
        {
            if (TryGetString(item, out var arrayValue))
            {
                yield return arrayValue;
            }
        }
    }

    private static bool TryGetString(JsonNode? value, out string result)
    {
        if (value is JsonValue scalar && scalar.TryGetValue<string>(out var text))
        {
            result = text;
            return true;
        }

        result = string.Empty;
        return false;
    }
}
