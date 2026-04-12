namespace InSpectra.Gen.Engine.Modes.Static.Projection;

using InSpectra.Gen.Engine.Modes.Static.Inspection;

using InSpectra.Gen.Engine.Contracts.Documents;
using InSpectra.Gen.Engine.Contracts.Signatures;

using InSpectra.Gen.Engine.Modes.Static.Metadata;

using System.Text.Json.Nodes;

internal sealed class StaticAnalysisOpenCliOptionBuilder
{
    public JsonArray? BuildOptions(StaticCommandDefinition? staticCommand, Document? helpDocument)
    {
        if (helpDocument is not null)
        {
            if (helpDocument.Options.Count > 0)
            {
                return BuildHelpAnchoredOptions(staticCommand, helpDocument);
            }

            if (!ShouldFallbackToStaticSurface(helpDocument))
            {
                return null;
            }
        }

        if (staticCommand?.Options.Count is not > 0)
        {
            return null;
        }

        return BuildStaticOnlyOptions(staticCommand.Options);
    }

    private JsonArray? BuildHelpAnchoredOptions(StaticCommandDefinition? staticCommand, Document helpDocument)
    {
        if (helpDocument.Options.Count is not > 0)
        {
            return null;
        }

        var array = new JsonArray();
        var matcher = new StaticOptionDefinitionMatcher(staticCommand?.Options ?? []);

        foreach (var helpOption in helpDocument.Options)
        {
            var signature = OptionSignatureSupport.Parse(helpOption.Key);
            var names = StaticAnalysisOpenCliNodeSupport.ParseHelpOptionNames(helpOption.Key);
            var definition = matcher.TakeMatch(names.LongName, names.ShortName);
            array.Add(BuildOptionNode(
                names.LongName is not null ? $"--{names.LongName}" : $"-{names.ShortName}",
                definition,
                helpOption.Description ?? definition?.Description,
                helpOption.IsRequired || definition?.IsRequired == true,
                signature.ArgumentName,
                signature.ArgumentName is not null
                    ? signature.ArgumentRequired
                    : definition is not null && !definition.IsBoolLike,
                names.LongName,
                names.ShortName));
        }

        return array.Count > 0 ? array : null;
    }

    private static bool ShouldFallbackToStaticSurface(Document helpDocument)
        => helpDocument.Options.Count == 0
            && helpDocument.Arguments.Count == 0
            && helpDocument.Commands.Count == 0;

    private JsonArray? BuildStaticOnlyOptions(IReadOnlyList<StaticOptionDefinition> options)
    {
        var array = new JsonArray();
        foreach (var option in options)
        {
            if (option.IsBoolLike && StaticAnalysisOpenCliNodeSupport.IsHiddenOption(option))
            {
                continue;
            }

            var primaryName = option.LongName is not null ? $"--{option.LongName}" : $"-{option.ShortName}";
            array.Add(BuildOptionNode(
                primaryName,
                option,
                option.Description,
                option.IsRequired,
                argumentName: null,
                argumentRequired: !option.IsBoolLike,
                option.LongName,
                option.ShortName));
        }

        return array.Count > 0 ? array : null;
    }

    private JsonObject BuildOptionNode(
        string name,
        StaticOptionDefinition? definition,
        string? description,
        bool required,
        string? argumentName,
        bool argumentRequired,
        string? longName,
        char? shortName)
    {
        var optionNode = new JsonObject
        {
            ["name"] = name,
            ["recursive"] = false,
            ["hidden"] = definition is not null && StaticAnalysisOpenCliNodeSupport.IsHiddenOption(definition),
        };
        StaticAnalysisOpenCliNodeSupport.AddIfPresent(optionNode, "description", description);

        var aliases = BuildAliases(definition, longName, shortName);
        if (aliases is not null)
        {
            optionNode["aliases"] = aliases;
        }

        var argument = BuildOptionArgument(definition, longName, shortName, argumentName, argumentRequired);
        if (argument is not null)
        {
            optionNode["arguments"] = new JsonArray { argument };
        }

        return optionNode;
    }

    private JsonObject? BuildOptionArgument(
        StaticOptionDefinition? definition,
        string? longName,
        char? shortName,
        string? argumentName,
        bool argumentRequired)
    {
        if (definition is null)
        {
            if (string.IsNullOrWhiteSpace(argumentName))
            {
                return null;
            }

            return new JsonObject
            {
                ["name"] = StaticAnalysisOpenCliNodeSupport.NormalizeArgumentName(argumentName),
                ["required"] = argumentRequired,
                ["arity"] = StaticAnalysisOpenCliNodeSupport.BuildArity(false, argumentRequired ? 1 : 0),
            };
        }

        if (definition.IsBoolLike)
        {
            return null;
        }

        var argument = new JsonObject
        {
            ["name"] = StaticAnalysisOpenCliNodeSupport.NormalizeArgumentName(
                argumentName
                ?? definition.MetaValue
                ?? definition.PropertyName
                ?? definition.LongName
                ?? shortName?.ToString()
                ?? "VALUE"),
            ["required"] = argumentRequired,
            ["arity"] = StaticAnalysisOpenCliNodeSupport.BuildArity(definition.IsSequence, argumentRequired ? 1 : 0),
        };

        StaticAnalysisOpenCliNodeSupport.ApplyInputMetadata(argument, definition.ClrType, definition.AcceptedValues);
        return argument;
    }

    private static JsonArray? BuildAliases(StaticOptionDefinition? definition, string? longName, char? shortName)
    {
        var aliases = new JsonArray();

        if (definition is not null)
        {
            if (longName is not null && definition.LongName is not null
                && !string.Equals(longName, definition.LongName, StringComparison.OrdinalIgnoreCase))
            {
                aliases.Add($"--{definition.LongName}");
            }

            if (shortName is not null && definition.ShortName is not null
                && shortName != definition.ShortName)
            {
                aliases.Add($"-{definition.ShortName}");
            }
            else if (shortName is null && definition.ShortName is not null)
            {
                aliases.Add($"-{definition.ShortName}");
            }
            else if (longName is null && definition.LongName is not null)
            {
                aliases.Add($"--{definition.LongName}");
            }
        }

        if (longName is not null && shortName is not null)
        {
            aliases.Add($"-{shortName}");
        }

        return aliases.Count > 0 ? aliases : null;
    }
}
