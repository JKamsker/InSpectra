namespace InSpectra.Gen.Acquisition.Modes.Static.Projection;

using InSpectra.Gen.Acquisition.Modes.Help.Projection;

using InSpectra.Gen.Acquisition.Contracts;
using InSpectra.Gen.Acquisition.OpenCli.Documents;
using InSpectra.Gen.Acquisition.OpenCli.Structure;
using InSpectra.Gen.Acquisition.Modes.Help.Documents;

using InSpectra.Gen.Acquisition.Modes.Static.Models;

using System.Text.Json.Nodes;

internal sealed class StaticAnalysisOpenCliBuilder
{
    private readonly OpenCliCommandTreeBuilder _commandTreeBuilder = new();
    private readonly StaticAnalysisOpenCliOptionBuilder _optionBuilder = new();
    private readonly StaticAnalysisOpenCliArgumentBuilder _argumentBuilder = new();

    public JsonObject Build(
        string commandName,
        string packageVersion,
        string framework,
        IReadOnlyDictionary<string, StaticCommandDefinition> staticCommands,
        IReadOnlyDictionary<string, Document> helpDocuments)
    {
        staticCommands = StaticAnalysisCommandPublishabilitySupport.FilterPublishableCommands(framework, staticCommands);
        helpDocuments.TryGetValue(string.Empty, out var rootHelp);
        staticCommands.TryGetValue(string.Empty, out var defaultCommand);

        var document = new JsonObject
        {
            ["opencli"] = "0.1-draft",
            ["info"] = BuildInfoNode(commandName, packageVersion, rootHelp, defaultCommand),
            ["x-inspectra"] = BuildExtensionMetadata(framework, staticCommands, helpDocuments),
        };

        var commandNodes = BuildCommandNodes(commandName, staticCommands, helpDocuments);
        if (commandNodes.Count > 0)
        {
            document["commands"] = commandNodes;
        }

        StaticAnalysisOpenCliNodeSupport.AddIfPresent(document, "options", _optionBuilder.BuildOptions(defaultCommand, rootHelp));
        StaticAnalysisOpenCliNodeSupport.AddIfPresent(document, "arguments", _argumentBuilder.BuildArguments(defaultCommand, rootHelp));
        return OpenCliDocumentSanitizer.Sanitize(document);
    }

    private static JsonObject BuildInfoNode(
        string commandName,
        string packageVersion,
        Document? rootHelp,
        StaticCommandDefinition? defaultCommand)
    {
        var info = new JsonObject
        {
            ["title"] = rootHelp?.Title ?? commandName,
            ["version"] = string.IsNullOrWhiteSpace(packageVersion) ? rootHelp?.Version : packageVersion,
        };
        StaticAnalysisOpenCliNodeSupport.AddIfPresent(
            info,
            "description",
            rootHelp?.ApplicationDescription ?? defaultCommand?.Description);
        return info;
    }

    private static JsonObject BuildExtensionMetadata(
        string framework,
        IReadOnlyDictionary<string, StaticCommandDefinition> staticCommands,
        IReadOnlyDictionary<string, Document> helpDocuments)
    {
        var optionCount = staticCommands.Values.Sum(c => c.Options.Count);
        var valueCount = staticCommands.Values.Sum(c => c.Values.Count);
        var verbCount = staticCommands.Count(pair => !string.IsNullOrEmpty(pair.Key));

        var limitations = new JsonArray
        {
            "property-defaults-not-captured",
            "fluent-api-configuration-not-visible",
        };

        return new JsonObject
        {
            ["artifactSource"] = "static-analysis",
            ["generator"] = InspectraProductInfo.GeneratorName,
            ["metadataEnriched"] = staticCommands.Count > 0,
            ["helpDocumentCount"] = helpDocuments.Count,
            ["staticAnalysis"] = new JsonObject
            {
                ["framework"] = framework,
                ["inspectorType"] = "dnlib",
                ["confidence"] = staticCommands.Count > 0 ? "high" : "low",
                ["verbCount"] = verbCount,
                ["optionCount"] = optionCount,
                ["valueCount"] = valueCount,
                ["limitations"] = limitations,
            },
        };
    }

    private JsonArray BuildCommandNodes(
        string commandName,
        IReadOnlyDictionary<string, StaticCommandDefinition> staticCommands,
        IReadOnlyDictionary<string, Document> helpDocuments)
    {
        var nodes = _commandTreeBuilder.Build(
            StaticAnalysisOpenCliCommandSelectionSupport.BuildCommandDescriptors(commandName, staticCommands, helpDocuments));
        return new JsonArray(nodes.Select(node => BuildCommandNode(node, staticCommands, helpDocuments)).ToArray());
    }

    private JsonObject BuildCommandNode(
        OpenCliCommandTreeNode commandNode,
        IReadOnlyDictionary<string, StaticCommandDefinition> staticCommands,
        IReadOnlyDictionary<string, Document> helpDocuments)
    {
        staticCommands.TryGetValue(commandNode.FullName, out var staticCommand);
        helpDocuments.TryGetValue(commandNode.FullName, out var helpDocument);

        var node = new JsonObject
        {
            ["name"] = commandNode.DisplayName,
            ["hidden"] = staticCommand?.IsHidden ?? false,
        };
        StaticAnalysisOpenCliNodeSupport.AddIfPresent(
            node,
            "description",
            helpDocument?.CommandDescription ?? staticCommand?.Description ?? commandNode.Description);
        StaticAnalysisOpenCliNodeSupport.AddIfPresent(node, "options", _optionBuilder.BuildOptions(staticCommand, helpDocument));
        StaticAnalysisOpenCliNodeSupport.AddIfPresent(node, "arguments", _argumentBuilder.BuildArguments(staticCommand, helpDocument));

        if (commandNode.Children.Count > 0)
        {
            node["commands"] = new JsonArray(commandNode.Children
                .Select(child => BuildCommandNode(child, staticCommands, helpDocuments))
                .ToArray());
        }

        return node;
    }
}
