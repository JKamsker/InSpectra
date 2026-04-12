namespace InSpectra.Lib.Modes.Help.Projection;

using InSpectra.Lib.Modes.Help.Inference.Descriptions;

using InSpectra.Lib.Modes.Help.Inference.Usage.Arguments;

using InSpectra.Lib.Contracts.Signatures;

using InSpectra.Lib.Contracts.Documents;

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

internal sealed partial class ArgumentNodeBuilder
{
    private static readonly HashSet<string> GenericArgumentNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ARG",
        "ARGS",
        "VALUE",
    };

    private static readonly HashSet<string> GenericDispatcherArgumentNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ARG",
        "ARGS",
        "COMMAND",
        "FILE",
        "PATH",
        "SUBCOMMAND",
        "TARGET",
        "VALUE",
    };

    public JsonArray? Build(string commandName, string commandPath, Document? helpDocument)
    {
        if (helpDocument is null)
        {
            return null;
        }

        var explicitArguments = helpDocument.Arguments;
        if (SignatureNormalizer.IsBuiltinAuxiliaryCommand(commandPath)
            && (UsageArgumentSupport.LooksLikeCommandInventoryEchoArguments(explicitArguments, helpDocument.Commands)
                || UsageArgumentSupport.LooksLikeAuxiliaryInventoryEchoArguments(explicitArguments, helpDocument.UsageLines)))
        {
            explicitArguments = [];
        }

        var usageArguments = UsageArgumentSupport.ExtractUsageArguments(
            commandName,
            commandPath,
            helpDocument.UsageLines,
            helpDocument.Commands.Count > 0);
        var arguments = UsageArgumentSupport.SelectArguments(explicitArguments, usageArguments);
        if (ShouldSuppressOptionShadowArguments(arguments, helpDocument.Options))
        {
            arguments = [];
        }

        if (ShouldSuppressDispatcherPlaceholderArguments(arguments, helpDocument))
        {
            arguments = [];
        }

        if (arguments.Count == 0)
        {
            if (SignatureNormalizer.IsBuiltinAuxiliaryCommand(commandPath))
            {
                return null;
            }

            arguments = OptionDescriptionArgumentInference.Infer(helpDocument.Options);
        }

        if (arguments.Count == 0)
        {
            return null;
        }

        var array = new JsonArray();
        foreach (var argument in arguments)
        {
            if (!TryParseArgumentSignature(argument.Key, out var signature))
            {
                continue;
            }

            var node = new JsonObject
            {
                ["name"] = signature.Name,
                ["required"] = argument.IsRequired,
                ["hidden"] = false,
                ["arity"] = BuildArity(argument.IsRequired ? 1 : 0, signature.IsSequence),
            };

            if (!string.IsNullOrWhiteSpace(argument.Description))
            {
                node["description"] = argument.Description;
            }

            array.Add(node);
        }

        return array.Count > 0 ? array : null;
    }

    public static bool TryParseArgumentSignature(string rawKey, out ArgumentSignature signature)
        => ArgumentSignatureParser.TryParse(rawKey, out signature);

    public static bool IsLowSignalExplicitArgument(Item argument)
        => TryParseArgumentSignature(argument.Key, out var signature)
            && GenericArgumentNames.Contains(signature.Name)
            && string.IsNullOrWhiteSpace(argument.Description);

    private static bool ShouldSuppressDispatcherPlaceholderArguments(
        IReadOnlyList<Item> arguments,
        Document helpDocument)
    {
        if (helpDocument.Commands.Count == 0
            || arguments.Count == 0
            || !LooksLikeDispatcherUsage(helpDocument.UsageLines))
        {
            return false;
        }

        return arguments.All(argument =>
            TryParseArgumentSignature(argument.Key, out var signature)
            && GenericDispatcherArgumentNames.Contains(signature.Name)
            && string.IsNullOrWhiteSpace(argument.Description));
    }

    private static bool ShouldSuppressOptionShadowArguments(
        IReadOnlyList<Item> arguments,
        IReadOnlyList<Item> options)
    {
        if (arguments.Count == 0 || options.Count == 0)
        {
            return false;
        }

        var optionArgumentNames = options
            .Select(GetOptionArgumentName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (optionArgumentNames.Count == 0)
        {
            return false;
        }

        foreach (var argument in arguments)
        {
            if (!TryParseArgumentSignature(argument.Key, out var signature)
                || !optionArgumentNames.Contains(signature.Name))
            {
                return false;
            }
        }

        return true;
    }

    private static bool LooksLikeDispatcherUsage(IReadOnlyList<string> usageLines)
    {
        foreach (var line in usageLines)
        {
            foreach (Match match in UsagePlaceholderRegex().Matches(line))
            {
                if (UsageArgumentPatternSupport.IsDispatcherPlaceholder(match.Groups["name"].Value))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static JsonObject BuildArity(int minimum, bool isSequence = false)
    {
        var arity = new JsonObject
        {
            ["minimum"] = minimum,
        };

        if (!isSequence)
        {
            arity["maximum"] = 1;
        }

        return arity;
    }

    private static string? GetOptionArgumentName(Item option)
    {
        var signature = OptionSignatureSupport.Parse(option.Key);
        return signature.ArgumentName ?? OptionDescriptionInference.InferArgumentName(signature, option.Description);
    }

    [GeneratedRegex(@"[\[<](?<name>[^\]>]+)[\]>]", RegexOptions.Compiled)]
    private static partial Regex UsagePlaceholderRegex();
}
