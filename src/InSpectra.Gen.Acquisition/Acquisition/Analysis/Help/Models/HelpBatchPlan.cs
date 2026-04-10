namespace InSpectra.Gen.Acquisition.Analysis.Help.Models;

using InSpectra.Gen.Acquisition.Infrastructure.Json;

using InSpectra.Gen.Acquisition.Analysis;

using System.Text.Json.Nodes;

internal sealed record HelpBatchPlan(string? BatchId, IReadOnlyList<HelpBatchItem> Items)
{
    public static HelpBatchPlan Load(string path)
    {
        var document = JsonNodeFileLoader.TryLoadJsonObject(path)
            ?? throw new InvalidOperationException($"Plan '{path}' is empty.");
        var itemsNode = document["items"]?.AsArray()
            ?? throw new InvalidOperationException($"Plan '{path}' is missing an 'items' array.");

        var items = itemsNode.OfType<JsonObject>().Select(ParseItem).ToList();
        return new HelpBatchPlan(document[ResultKey.BatchId]?.GetValue<string>(), items);
    }

    private static HelpBatchItem ParseItem(JsonObject item)
        => new(
            PackageId: ReadRequiredString(item, "packageId"),
            Version: ReadRequiredString(item, "version"),
            CommandName: item["command"]?.GetValue<string>(),
            CliFramework: item[ResultKey.CliFramework]?.GetValue<string>(),
            AnalysisMode: item[ResultKey.AnalysisMode]?.GetValue<string>() ?? AnalysisMode.Help,
            ExpectedCommands: ReadStringList(item, "expectedCommands"),
            ExpectedOptions: ReadStringList(item, "expectedOptions"),
            ExpectedArguments: ReadStringList(item, "expectedArguments"),
            Attempt: item[ResultKey.Attempt]?.GetValue<int?>() ?? 1,
            ArtifactName: item["artifactName"]?.GetValue<string>(),
            PackageUrl: item["packageUrl"]?.GetValue<string>(),
            PackageContentUrl: item["packageContentUrl"]?.GetValue<string>(),
            CatalogEntryUrl: item["catalogEntryUrl"]?.GetValue<string>(),
            TotalDownloads: item["totalDownloads"]?.GetValue<long?>());

    private static string ReadRequiredString(JsonObject item, string propertyName)
        => item[propertyName]?.GetValue<string>() is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Plan item is missing required property '{propertyName}'.");

    private static IReadOnlyList<string> ReadStringList(JsonObject item, string propertyName)
        => item[propertyName] is not JsonArray values
            ? []
            : values.OfType<JsonValue>()
                .Select(value => value.GetValue<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
}
