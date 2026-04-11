namespace InSpectra.Gen.Acquisition.Tooling.Json;

using System.Text.Json.Nodes;

internal static class JsonNodeFileLoader
{
    public static JsonObject? TryLoadJsonObject(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
        }
        catch
        {
            return null;
        }
    }
}

