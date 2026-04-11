using System.Xml;
using System.Xml.Linq;
using InSpectra.Gen.Core;

namespace InSpectra.Gen.OpenCli.Enrichment;

public sealed class OpenCliXmlEnricher
{
    public async Task<XmlEnrichmentResult> EnrichFromFileAsync(OpenCliDocument document, string path, CancellationToken cancellationToken)
    {
        var resolvedPath = Path.GetFullPath(path);
        if (!File.Exists(resolvedPath))
        {
            throw new CliUsageException($"XML enrichment file `{resolvedPath}` does not exist.");
        }

        var xml = await File.ReadAllTextAsync(resolvedPath, cancellationToken);
        return EnrichFromXml(document, xml, resolvedPath);
    }

    public XmlEnrichmentResult EnrichFromXml(OpenCliDocument document, string xml, string sourceLabel)
    {
        XDocument parsed;

        try
        {
            parsed = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        }
        catch (Exception exception) when (exception is XmlException or InvalidOperationException)
        {
            throw new CliDataException($"XML enrichment source `{sourceLabel}` is not valid XML.", [exception.Message], exception);
        }

        var root = parsed.Root;
        if (root is null || !string.Equals(root.Name.LocalName, "Model", StringComparison.Ordinal))
        {
            throw new CliDataException($"XML enrichment source `{sourceLabel}` does not contain a `<Model>` root element.");
        }

        var xmlCommands = new Dictionary<string, XmlCommandInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var commandElement in root.Elements("Command"))
        {
            IndexCommand(commandElement, null, xmlCommands);
        }

        var summary = new XmlEnrichmentResult();
        foreach (var command in document.Commands)
        {
            EnrichCommand(command, null, xmlCommands, summary);
        }

        if (summary.MatchedCommandCount == 0)
        {
            summary.Warnings.Add($"No XML command descriptions from `{sourceLabel}` matched the OpenCLI document.");
        }

        return summary;
    }

    private static void IndexCommand(XElement element, string? parentPath, IDictionary<string, XmlCommandInfo> index)
    {
        var name = (string?)element.Attribute("Name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var path = string.IsNullOrWhiteSpace(parentPath)
            ? name
            : $"{parentPath} {name}";
        var parameters = new List<XmlParameterInfo>();
        var parametersElement = element.Element("Parameters");
        if (parametersElement is not null)
        {
            foreach (var parameterElement in parametersElement.Elements())
            {
                parameters.Add(new XmlParameterInfo
                {
                    Kind = parameterElement.Name.LocalName,
                    LongName = (string?)parameterElement.Attribute("Long"),
                    ShortName = (string?)parameterElement.Attribute("Short"),
                    Name = (string?)parameterElement.Attribute("Name"),
                    Description = NormalizeText(parameterElement.Element("Description")?.Value),
                });
            }
        }

        index[path] = new XmlCommandInfo
        {
            Description = NormalizeText(element.Element("Description")?.Value),
            Parameters = parameters,
        };

        foreach (var child in element.Elements("Command"))
        {
            IndexCommand(child, path, index);
        }
    }

    private static void EnrichCommand(
        OpenCliCommand command,
        string? parentPath,
        IReadOnlyDictionary<string, XmlCommandInfo> index,
        XmlEnrichmentResult summary)
    {
        var path = string.IsNullOrWhiteSpace(parentPath)
            ? command.Name
            : $"{parentPath} {command.Name}";

        if (index.TryGetValue(path, out var xmlCommand))
        {
            summary.MatchedCommandCount++;

            if (string.IsNullOrWhiteSpace(command.Description) && !string.IsNullOrWhiteSpace(xmlCommand.Description))
            {
                command.Description = xmlCommand.Description;
                summary.EnrichedDescriptionCount++;
            }

            foreach (var option in command.Options)
            {
                var match = MatchOption(option, xmlCommand.Parameters);
                if (match is not null && string.IsNullOrWhiteSpace(option.Description) && !string.IsNullOrWhiteSpace(match.Description))
                {
                    option.Description = match.Description;
                    summary.EnrichedDescriptionCount++;
                }
            }

            foreach (var argument in command.Arguments)
            {
                var match = MatchArgument(argument, xmlCommand.Parameters);
                if (match is not null && string.IsNullOrWhiteSpace(argument.Description) && !string.IsNullOrWhiteSpace(match.Description))
                {
                    argument.Description = match.Description;
                    summary.EnrichedDescriptionCount++;
                }
            }
        }

        foreach (var child in command.Commands)
        {
            EnrichCommand(child, path, index, summary);
        }
    }

    private static XmlParameterInfo? MatchOption(OpenCliOption option, IReadOnlyList<XmlParameterInfo> parameters)
    {
        var longName = option.Name.TrimStart('-');
        foreach (var parameter in parameters)
        {
            if (string.Equals(parameter.Kind, "Option", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(parameter.LongName, longName, StringComparison.OrdinalIgnoreCase) ||
                 option.Aliases.Any(alias => string.Equals(alias.TrimStart('-'), parameter.ShortName, StringComparison.OrdinalIgnoreCase))))
            {
                return parameter;
            }
        }

        return null;
    }

    private static XmlParameterInfo? MatchArgument(OpenCliArgument argument, IReadOnlyList<XmlParameterInfo> parameters)
    {
        return parameters.FirstOrDefault(parameter =>
            string.Equals(parameter.Kind, "Argument", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(parameter.Name, argument.Name, StringComparison.Ordinal));
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private sealed class XmlCommandInfo
    {
        public string? Description { get; init; }

        public required IReadOnlyList<XmlParameterInfo> Parameters { get; init; }
    }

    private sealed class XmlParameterInfo
    {
        public required string Kind { get; init; }

        public string? LongName { get; init; }

        public string? ShortName { get; init; }

        public string? Name { get; init; }

        public string? Description { get; init; }
    }
}
