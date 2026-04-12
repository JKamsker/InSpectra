namespace InSpectra.Gen.Engine.Tooling.DocumentPipeline.Structure;

using System.Text.Json.Nodes;

internal static class OpenCliNodeValidationSupport
{
    private const int MaxCommandPathRecurrence = 3;
    private static readonly string[] CommandLikeArrayProperties = ["arguments", "commands", "examples", "options"];

    public static bool TryValidateCommandLikeNode(JsonObject node, string path, bool isRoot, out string? reason)
        => TryValidateCommandLikeNode(node, path, isRoot, [], out reason);

    private static bool TryValidateCommandLikeNode(
        JsonObject node,
        string path,
        bool isRoot,
        IReadOnlyList<string> ancestorCommandNames,
        out string? reason)
    {
        reason = null;

        foreach (var arrayProperty in CommandLikeArrayProperties)
        {
            if (!OpenCliValidationSupport.TryValidateArrayProperty(node, arrayProperty, path, out reason))
            {
                return false;
            }
        }

        if (node["examples"] is JsonArray examples
            && !OpenCliValidationSupport.TryValidateStringEntries(examples, $"{path}.examples", out reason))
        {
            return false;
        }

        if (!isRoot && string.Equals(OpenCliValidationSupport.GetString(node["name"]), "__default_command", StringComparison.Ordinal))
        {
            reason = $"OpenCLI artifact contains a '__default_command' node at '{path}'.";
            return false;
        }

        var commandName = GetValidatedCommandName(node, path, isRoot, ancestorCommandNames, out reason);
        if (reason is not null)
        {
            return false;
        }

        var childAncestorCommandNames = commandName is null
            ? ancestorCommandNames
            : [.. ancestorCommandNames, commandName];

        if (!OpenCliNodeInputValidationSupport.TryValidateOptions(node["options"] as JsonArray, path, out reason))
        {
            return false;
        }

        if (!OpenCliNodeInputValidationSupport.TryValidateArguments(node["arguments"] as JsonArray, $"{path}.arguments", out reason))
        {
            return false;
        }

        if (!TryValidateCommands(node["commands"] as JsonArray, $"{path}.commands", childAncestorCommandNames, out reason))
        {
            return false;
        }

        return true;
    }
    private static bool TryValidateCommands(
        JsonArray? commands,
        string pathPrefix,
        IReadOnlyList<string> ancestorCommandNames,
        out string? reason)
    {
        reason = null;
        if (commands is null)
        {
            return true;
        }

        for (var index = 0; index < commands.Count; index++)
        {
            var commandPath = $"{pathPrefix}[{index}]";
            if (commands[index] is not JsonObject command)
            {
                reason = $"OpenCLI artifact has a non-object entry at '{commandPath}'.";
                return false;
            }

            if (!TryValidateCommandLikeNode(command, commandPath, isRoot: false, ancestorCommandNames, out reason))
            {
                return false;
            }
        }

        return true;
    }

    private static string? GetValidatedCommandName(
        JsonObject node,
        string path,
        bool isRoot,
        IReadOnlyList<string> ancestorCommandNames,
        out string? reason)
    {
        reason = null;
        if (isRoot)
        {
            return null;
        }

        var commandName = OpenCliValidationSupport.GetString(node["name"])?.Trim();
        if (!OpenCliNameValidationSupport.TryRequireCommandName(commandName, path, out reason))
        {
            return null;
        }

        if (commandName is null)
        {
            return null;
        }

        var recurrenceCount = CountCommandNameOccurrences(ancestorCommandNames, commandName) + 1;
        if (recurrenceCount <= MaxCommandPathRecurrence)
        {
            return commandName;
        }

        reason = $"OpenCLI artifact repeats command name '{commandName}' more than {MaxCommandPathRecurrence} times within the same command path at '{path}'.";
        return null;
    }

    private static int CountCommandNameOccurrences(IReadOnlyList<string> commandNames, string candidate)
    {
        var count = 0;
        foreach (var commandName in commandNames)
        {
            if (string.Equals(commandName, candidate, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }
}
