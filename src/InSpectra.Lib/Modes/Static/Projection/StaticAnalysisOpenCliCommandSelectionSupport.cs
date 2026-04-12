namespace InSpectra.Lib.Modes.Static.Projection;

using InSpectra.Lib.Contracts.Documents;
using InSpectra.Lib.Contracts.Providers;
using InSpectra.Lib.Tooling.DocumentPipeline.Documents;
using InSpectra.Lib.Tooling.DocumentPipeline.Structure;
using InSpectra.Lib.Modes.Static.Metadata;

internal static class StaticAnalysisOpenCliCommandSelectionSupport
{
    public static IEnumerable<OpenCliCommandDescriptor> BuildCommandDescriptors(
        string commandName,
        IReadOnlyDictionary<string, StaticCommandDefinition> staticCommands,
        IReadOnlyDictionary<string, Document> helpDocuments)
    {
        var restrictStaticCommands = ShouldRestrictStaticCommandsToHelpGraph(helpDocuments);
        var helpCommandKeys = restrictStaticCommands
            ? BuildHelpCommandKeySet(commandName, helpDocuments)
            : null;
        var includeHelpDerivedCommands = ShouldIncludeHelpDerivedCommands(commandName, staticCommands, helpDocuments);

        foreach (var pair in staticCommands.Where(pair => !string.IsNullOrWhiteSpace(pair.Key)))
        {
            if (IsPlaceholderStaticRootCommand(pair.Key, pair.Value, staticCommands, helpDocuments))
            {
                continue;
            }

            if (restrictStaticCommands
                && helpCommandKeys is not null
                && !ShouldIncludeStaticCommand(pair.Key, helpCommandKeys))
            {
                continue;
            }

            yield return new OpenCliCommandDescriptor(pair.Key, pair.Value.Description);
        }

        if (!includeHelpDerivedCommands)
        {
            yield break;
        }

        foreach (var pair in helpDocuments)
        {
            if (DocumentInspector.IsBuiltinAuxiliaryInventoryEcho(pair.Key, pair.Value))
            {
                continue;
            }

            if (!ShouldIncludeHelpChildCommands(commandName, pair.Key, pair.Value, staticCommands))
            {
                continue;
            }

            foreach (var child in pair.Value.Commands)
            {
                var childFullName = CommandPathSupport.ResolveChildKey(commandName, pair.Key, child.Key);
                if (!DocumentInspector.IsBuiltinAuxiliaryCommandPath(childFullName))
                {
                    yield return new OpenCliCommandDescriptor(childFullName, child.Description);
                }
            }
        }

        foreach (var pair in helpDocuments.Where(pair => !string.IsNullOrWhiteSpace(pair.Key)))
        {
            if (ShouldIncludeHelpDocumentCommandSurface(commandName, pair.Key, pair.Value, staticCommands))
            {
                yield return new OpenCliCommandDescriptor(pair.Key, pair.Value.CommandDescription);
            }
        }
    }

    private static bool IsPlaceholderStaticRootCommand(
        string commandKey,
        StaticCommandDefinition definition,
        IReadOnlyDictionary<string, StaticCommandDefinition> staticCommands,
        IReadOnlyDictionary<string, Document> helpDocuments)
        => string.Equals(commandKey, "root", StringComparison.OrdinalIgnoreCase)
            && (staticCommands.Count > 1 || helpDocuments.Count > 0)
            && !helpDocuments.ContainsKey(commandKey)
            && string.IsNullOrWhiteSpace(definition.Description)
            && definition.Options.Count == 0
            && definition.Values.Count == 0;

    private static bool ShouldRestrictStaticCommandsToHelpGraph(IReadOnlyDictionary<string, Document> helpDocuments)
        => helpDocuments.TryGetValue(string.Empty, out var rootHelp)
            && rootHelp.Commands.Count > 0;

    private static bool ShouldIncludeHelpDerivedCommands(
        string commandName,
        IReadOnlyDictionary<string, StaticCommandDefinition> staticCommands,
        IReadOnlyDictionary<string, Document> helpDocuments)
    {
        if (staticCommands.Count == 0)
        {
            return helpDocuments.Keys.Any(key => !string.IsNullOrWhiteSpace(key))
                || helpDocuments.TryGetValue(string.Empty, out var helpOnlyRoot)
                    && (helpOnlyRoot.Commands.Count > 0
                        || UsageCommandHubDetectorAccessor.Current.LooksLikeCommandHub(commandName, helpOnlyRoot.UsageLines));
        }

        if (staticCommands.Keys.Any(key => !string.IsNullOrWhiteSpace(key)))
        {
            return true;
        }

        if (!helpDocuments.TryGetValue(string.Empty, out var rootHelp))
        {
            return helpDocuments.Keys.Any(key => !string.IsNullOrWhiteSpace(key));
        }

        return rootHelp.Commands.Count > 0
            || UsageCommandHubDetectorAccessor.Current.LooksLikeCommandHub(commandName, rootHelp.UsageLines);
    }

    private static bool ShouldIncludeHelpChildCommands(
        string commandName,
        string commandPath,
        Document document,
        IReadOnlyDictionary<string, StaticCommandDefinition> staticCommands)
    {
        if (document.Commands.Count > 0
            || UsageCommandHubDetectorAccessor.Current.LooksLikeCommandHub(commandName, document.UsageLines))
        {
            return true;
        }

        if (staticCommands.Keys.Any(key =>
                !string.IsNullOrWhiteSpace(key)
                && key.StartsWith((string.IsNullOrWhiteSpace(commandPath) ? string.Empty : commandPath + " "), StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return document.Options.Count == 0 && document.Arguments.Count == 0;
    }

    private static bool ShouldIncludeHelpDocumentCommandSurface(
        string commandName,
        string commandPath,
        Document document,
        IReadOnlyDictionary<string, StaticCommandDefinition> staticCommands)
    {
        if (UsageCommandHubDetectorAccessor.Current.LooksLikeCommandHub(commandName, document.UsageLines))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(commandPath))
        {
            return document.Options.Count == 0 && document.Arguments.Count == 0;
        }

        if (staticCommands.ContainsKey(commandPath))
        {
            return true;
        }

        if (staticCommands.Keys.Any(key =>
                !string.IsNullOrWhiteSpace(key)
                && key.StartsWith(commandPath + " ", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return document.Options.Count == 0 && document.Arguments.Count == 0;
    }

    private static HashSet<string> BuildHelpCommandKeySet(
        string commandName,
        IReadOnlyDictionary<string, Document> helpDocuments)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in helpDocuments)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                keys.Add(pair.Key);
            }

            foreach (var child in pair.Value.Commands)
            {
                var childFullName = CommandPathSupport.ResolveChildKey(commandName, pair.Key, child.Key);
                if (!string.IsNullOrWhiteSpace(childFullName)
                    && !DocumentInspector.IsBuiltinAuxiliaryCommandPath(childFullName))
                {
                    keys.Add(childFullName);
                }
            }
        }

        return keys;
    }

    private static bool ShouldIncludeStaticCommand(string commandKey, IReadOnlySet<string> helpCommandKeys)
    {
        if (helpCommandKeys.Contains(commandKey))
        {
            return true;
        }

        return helpCommandKeys.Any(helpKey =>
            helpKey.StartsWith(commandKey + " ", StringComparison.OrdinalIgnoreCase)
            || commandKey.StartsWith(helpKey + " ", StringComparison.OrdinalIgnoreCase));
    }
}
