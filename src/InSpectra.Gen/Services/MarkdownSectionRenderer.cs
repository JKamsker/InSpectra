using System.Text;
using InSpectra.Gen.Models;

namespace InSpectra.Gen.Services;

public sealed class MarkdownSectionRenderer(
    MarkdownTableRenderer tableRenderer,
    MarkdownMetadataRenderer metadataRenderer,
    RenderModelFormatter formatter,
    CommandPathResolver pathResolver)
{
    public void AppendInfoSection(OpenCliDocument document, StringBuilder builder)
    {
        AppendConventions(document, builder);
        AppendContact(document, builder);
        AppendLicense(document, builder);
        AppendExamples(document.Examples, "### Examples", builder);

        if (document.ExitCodes.Count > 0)
        {
            builder.AppendLine("### Exit Codes");
            builder.AppendLine();
            tableRenderer.AppendExitCodeTable(document.ExitCodes, builder);
        }
    }

    public void AppendCommandBody(
        NormalizedCommand command,
        StringBuilder builder,
        bool includeMetadata,
        int headingLevel,
        string? currentPagePath,
        HybridLinkContext? hybridContext = null)
    {
        AppendDescription(command.Command.Description, builder);
        AppendAttributes(command.Command, builder);
        AppendSubcommands(command, builder, headingLevel, currentPagePath, hybridContext);
        AppendArguments(command, builder, headingLevel);
        AppendOptions(command, builder, headingLevel);
        AppendExamples(command.Command.Examples, $"{new string('#', headingLevel)} Examples", builder);

        if (command.Command.ExitCodes.Count > 0)
        {
            builder.AppendLine($"{new string('#', headingLevel)} Exit Codes");
            builder.AppendLine();
            tableRenderer.AppendExitCodeTable(command.Command.ExitCodes, builder);
        }

        if (includeMetadata)
        {
            metadataRenderer.AppendCommandMetadata(command, builder, headingLevel);
        }
    }

    private static void AppendConventions(OpenCliDocument document, StringBuilder builder)
    {
        if (document.Conventions is null)
        {
            return;
        }

        builder.AppendLine("### Conventions");
        builder.AppendLine();
        builder.AppendLine($"- Group short options: `{document.Conventions.GroupOptions?.ToString() ?? "unspecified"}`");
        builder.AppendLine($"- Option separator: `{document.Conventions.OptionSeparator ?? "unspecified"}`");
        builder.AppendLine();
    }

    private static void AppendContact(OpenCliDocument document, StringBuilder builder)
    {
        if (document.Info.Contact is null)
        {
            return;
        }

        builder.AppendLine("### Contact");
        builder.AppendLine();
        if (!string.IsNullOrWhiteSpace(document.Info.Contact.Name))
        {
            builder.AppendLine($"- Name: {document.Info.Contact.Name}");
        }

        if (!string.IsNullOrWhiteSpace(document.Info.Contact.Email))
        {
            builder.AppendLine($"- Email: `{document.Info.Contact.Email}`");
        }

        if (!string.IsNullOrWhiteSpace(document.Info.Contact.Url))
        {
            builder.AppendLine($"- URL: {document.Info.Contact.Url}");
        }

        builder.AppendLine();
    }

    private static void AppendLicense(OpenCliDocument document, StringBuilder builder)
    {
        if (document.Info.License is null)
        {
            return;
        }

        builder.AppendLine("### License");
        builder.AppendLine();
        if (!string.IsNullOrWhiteSpace(document.Info.License.Name))
        {
            builder.AppendLine($"- Name: {document.Info.License.Name}");
        }

        if (!string.IsNullOrWhiteSpace(document.Info.License.Identifier))
        {
            builder.AppendLine($"- Identifier: `{document.Info.License.Identifier}`");
        }

        if (!string.IsNullOrWhiteSpace(document.Info.License.Url))
        {
            builder.AppendLine($"- URL: {document.Info.License.Url}");
        }

        builder.AppendLine();
    }

    private static void AppendDescription(string? description, StringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        builder.AppendLine(description);
        builder.AppendLine();
    }

    private void AppendAttributes(OpenCliCommand command, StringBuilder builder)
    {
        var attributes = formatter.BuildCommandAttributes(command)
            .Select(attribute => attribute.StartsWith("Aliases:", StringComparison.Ordinal) ? FormatAliasAttribute(attribute) : attribute)
            .ToList();
        if (attributes.Count == 0)
        {
            return;
        }

        foreach (var attribute in attributes)
        {
            builder.AppendLine($"- {attribute}");
        }

        builder.AppendLine();
    }

    private void AppendSubcommands(NormalizedCommand command, StringBuilder builder, int headingLevel, string? currentPagePath, HybridLinkContext? hybridContext)
    {
        if (command.Commands.Count == 0)
        {
            return;
        }

        builder.AppendLine($"{new string('#', headingLevel)} Subcommands");
        builder.AppendLine();
        foreach (var child in command.Commands)
        {
            string line;
            if (hybridContext is not null)
            {
                line = $"- [{child.Command.Name}]({hybridContext.ResolveTarget(child)}){formatter.FormatDescriptionSuffix(child.Command.Description)}";
            }
            else if (currentPagePath is null)
            {
                line = $"- `{child.Command.Name}`{formatter.FormatDescriptionSuffix(child.Command.Description)}";
            }
            else
            {
                line = $"- [{child.Command.Name}]({pathResolver.CreateRelativeLink(currentPagePath, pathResolver.GetCommandRelativePath(child, "md"))}){formatter.FormatDescriptionSuffix(child.Command.Description)}";
            }

            builder.AppendLine(line);
        }

        builder.AppendLine();
    }

    private void AppendArguments(NormalizedCommand command, StringBuilder builder, int headingLevel)
    {
        if (command.Arguments.Count == 0)
        {
            return;
        }

        builder.AppendLine($"{new string('#', headingLevel)} Arguments");
        builder.AppendLine();
        tableRenderer.AppendArgumentTable(command.Arguments, builder);
    }

    private void AppendOptions(NormalizedCommand command, StringBuilder builder, int headingLevel)
    {
        if (command.DeclaredOptions.Count == 0 && command.InheritedOptions.Count == 0)
        {
            return;
        }

        builder.AppendLine($"{new string('#', headingLevel)} Options");
        builder.AppendLine();
        tableRenderer.AppendOptionTable(
            command.DeclaredOptions.Select(option => new ResolvedOption
            {
                Option = option,
                IsInherited = false,
            }).Concat(command.InheritedOptions),
            builder);
    }

    private static void AppendExamples(IEnumerable<string> examples, string heading, StringBuilder builder)
    {
        var exampleList = examples.ToList();
        if (exampleList.Count == 0)
        {
            return;
        }

        builder.AppendLine(heading);
        builder.AppendLine();
        foreach (var example in exampleList)
        {
            builder.AppendLine($"- `{example}`");
        }

        builder.AppendLine();
    }

    private static string FormatAliasAttribute(string attribute)
    {
        var aliases = attribute["Aliases: ".Length..]
            .Split(", ", StringSplitOptions.RemoveEmptyEntries)
            .Select(alias => $"`{alias}`");
        return $"Aliases: {string.Join(", ", aliases)}";
    }
}
