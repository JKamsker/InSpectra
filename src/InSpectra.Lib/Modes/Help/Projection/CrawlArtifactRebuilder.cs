using System.Text.Json.Nodes;

using InSpectra.Lib.Contracts.Documents;
using InSpectra.Lib.Contracts.Providers;
using InSpectra.Lib.Modes.Help.Crawling;
using InSpectra.Lib.Modes.Help.Parsing;
using InSpectra.Lib.Tooling.DocumentPipeline.Documents;

namespace InSpectra.Lib.Modes.Help.Projection;

/// <summary>
/// Rebuilds an OpenCLI document from a stored crawl capture by replaying the
/// capture payloads through the parsing and OpenCLI building pipeline.
/// </summary>
internal sealed class CrawlArtifactRebuilder(OpenCliBuilder openCliBuilder) : ICrawlArtifactRebuilder
{
    private readonly TextParser _parser = new();

    public JsonObject? RebuildOpenCli(
        JsonObject crawlJson,
        string commandName,
        string version,
        string? cliFramework = null)
    {
        var captures = crawlJson["commands"] as JsonArray;
        var parsedDocuments = ParseCaptures(commandName, captures);

        if (!parsedDocuments.TryGetValue(string.Empty, out _))
        {
            parsedDocuments[string.Empty] = CreateEmptyRootDocument();
        }

        var reachableDocuments = BuildReachableDocuments(commandName, parsedDocuments);
        if (reachableDocuments.Count == 0)
        {
            reachableDocuments[string.Empty] = CreateEmptyRootDocument();
        }

        var openCli = openCliBuilder.Build(commandName, version, reachableDocuments);
        if (!string.IsNullOrWhiteSpace(cliFramework))
        {
            openCli["x-inspectra"]!.AsObject()["cliFramework"] = cliFramework;
        }

        return openCli;
    }

    private Dictionary<string, Document> ParseCaptures(string rootCommandName, JsonArray? captures)
    {
        var documents = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase);
        foreach (var capture in captures?.OfType<JsonObject>() ?? [])
        {
            var selected = CapturePayloadSupport.SelectBestDocument(_parser, rootCommandName, capture);
            if (selected is null)
            {
                continue;
            }

            if (!documents.TryGetValue(selected.CommandKey, out var existing)
                || DocumentInspector.Score(selected.Document) > DocumentInspector.Score(existing))
            {
                documents[selected.CommandKey] = selected.Document;
            }
        }

        return documents;
    }

    private static Dictionary<string, Document> BuildReachableDocuments(
        string rootCommandName,
        IReadOnlyDictionary<string, Document> parsedCaptures)
    {
        var documents = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase);
        if (!parsedCaptures.TryGetValue(string.Empty, out var rootDocument))
        {
            return documents;
        }

        var queue = new Queue<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { string.Empty };
        documents[string.Empty] = rootDocument;
        queue.Enqueue(string.Empty);

        while (queue.Count > 0)
        {
            var commandKey = queue.Dequeue();
            var current = documents[commandKey];
            if (DocumentInspector.IsBuiltinAuxiliaryInventoryEcho(commandKey, current))
            {
                continue;
            }

            foreach (var child in current.Commands)
            {
                var childKey = CommandPathSupport.ResolveChildKey(rootCommandName, commandKey, child.Key);
                if (!seen.Add(childKey) || !parsedCaptures.TryGetValue(childKey, out var childDocument))
                {
                    continue;
                }

                documents[childKey] = childDocument;
                queue.Enqueue(childKey);
            }
        }

        return documents;
    }

    private static Document CreateEmptyRootDocument()
        => new(
            Title: null,
            Version: null,
            ApplicationDescription: null,
            CommandDescription: null,
            UsageLines: [],
            Arguments: [],
            Options: [],
            Commands: []);
}
