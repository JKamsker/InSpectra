using System.Text;
using System.Text.Json;
using OpenCli.Renderer.Models;

namespace OpenCli.Renderer.Services;

public sealed class MarkdownRenderer
{
    public string RenderSingle(NormalizedCliDocument document, bool includeMetadata)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {document.Source.Info.Title}");
        builder.AppendLine();
        builder.AppendLine($"- Version: `{document.Source.Info.Version}`");
        builder.AppendLine($"- OpenCLI: `{document.Source.OpenCliVersion}`");

        if (!string.IsNullOrWhiteSpace(document.Source.Info.Summary))
        {
            builder.AppendLine();
            builder.AppendLine(document.Source.Info.Summary);
        }

        if (!string.IsNullOrWhiteSpace(document.Source.Info.Description))
        {
            builder.AppendLine();
            builder.AppendLine(document.Source.Info.Description);
        }

        builder.AppendLine();
        builder.AppendLine("## Table of Contents");
        builder.AppendLine();
        builder.AppendLine("- [Overview](#overview)");
        if (document.RootArguments.Count > 0)
        {
            builder.AppendLine("- [Root Arguments](#root-arguments)");
        }

        if (document.RootOptions.Count > 0)
        {
            builder.AppendLine("- [Root Options](#root-options)");
        }

        if (document.Commands.Count > 0)
        {
            builder.AppendLine("- [Commands](#commands)");
            AppendCommandToc(document.Commands, builder, 1);
        }

        builder.AppendLine();
        builder.AppendLine("<a id=\"overview\"></a>");
        builder.AppendLine("## Overview");
        builder.AppendLine();
        AppendInfoSection(document.Source, builder);

        if (document.RootArguments.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("<a id=\"root-arguments\"></a>");
            builder.AppendLine("## Root Arguments");
            builder.AppendLine();
            AppendArgumentTable(document.RootArguments, builder);
        }

        if (document.RootOptions.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("<a id=\"root-options\"></a>");
            builder.AppendLine("## Root Options");
            builder.AppendLine();
            AppendOptionTable(document.RootOptions.Select(option => new ResolvedOption
            {
                Option = option,
                IsInherited = false,
            }), builder);
        }

        if (document.Commands.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("<a id=\"commands\"></a>");
            builder.AppendLine("## Commands");
            builder.AppendLine();
            AppendCommandSections(document.Commands, builder, includeMetadata, 2);
        }

        if (includeMetadata)
        {
            AppendRootMetadata(document.Source, builder);
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    public IReadOnlyList<RelativeMarkdownFile> RenderTree(NormalizedCliDocument document, bool includeMetadata)
    {
        var files = new List<RelativeMarkdownFile>
        {
            new("index.md", RenderRootPage(document, includeMetadata)),
        };

        foreach (var command in document.Commands)
        {
            AppendCommandPages(command, includeMetadata, files);
        }

        return files;
    }

    private static void AppendCommandToc(IEnumerable<NormalizedCommand> commands, StringBuilder builder, int depth)
    {
        var prefix = new string(' ', depth * 2);
        foreach (var command in commands)
        {
            builder.AppendLine($"{prefix}- [{command.Path}](#command-{CreateAnchorId(command.Path)})");
            AppendCommandToc(command.Commands, builder, depth + 1);
        }
    }

    private static void AppendCommandSections(
        IEnumerable<NormalizedCommand> commands,
        StringBuilder builder,
        bool includeMetadata,
        int headingLevel)
    {
        foreach (var command in commands)
        {
            builder.AppendLine($"<a id=\"command-{CreateAnchorId(command.Path)}\"></a>");
            builder.AppendLine($"{new string('#', headingLevel)} `{command.Path}`");
            builder.AppendLine();
            AppendCommandBody(command, builder, includeMetadata, headingLevel + 1, null);
            AppendCommandSections(command.Commands, builder, includeMetadata, headingLevel + 1);
        }
    }

    private static string RenderRootPage(NormalizedCliDocument document, bool includeMetadata)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {document.Source.Info.Title}");
        builder.AppendLine();
        builder.AppendLine($"- Version: `{document.Source.Info.Version}`");
        builder.AppendLine($"- OpenCLI: `{document.Source.OpenCliVersion}`");
        builder.AppendLine();
        AppendInfoSection(document.Source, builder);

        if (document.RootArguments.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Root Arguments");
            builder.AppendLine();
            AppendArgumentTable(document.RootArguments, builder);
        }

        if (document.RootOptions.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Root Options");
            builder.AppendLine();
            AppendOptionTable(document.RootOptions.Select(option => new ResolvedOption
            {
                Option = option,
                IsInherited = false,
            }), builder);
        }

        builder.AppendLine();
        builder.AppendLine("## Commands");
        builder.AppendLine();
        foreach (var command in document.Commands)
        {
            builder.AppendLine($"- [{command.Command.Name}]({GetCommandRelativePath(command)}){FormatDescriptionSuffix(command.Command.Description)}");
        }

        if (includeMetadata)
        {
            AppendRootMetadata(document.Source, builder);
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static void AppendCommandPages(
        NormalizedCommand command,
        bool includeMetadata,
        ICollection<RelativeMarkdownFile> files)
    {
        var relativePath = GetCommandRelativePath(command);
        files.Add(new RelativeMarkdownFile(relativePath, RenderCommandPage(command, includeMetadata, relativePath)));

        foreach (var child in command.Commands)
        {
            AppendCommandPages(child, includeMetadata, files);
        }
    }

    private static string RenderCommandPage(
        NormalizedCommand command,
        bool includeMetadata,
        string relativePath)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# `{command.Path}`");
        builder.AppendLine();
        builder.AppendLine($"- Root: [index]({CreateRelativeLink(relativePath, "index.md")})");

        var parentPath = GetParentRelativePath(command);
        if (parentPath is not null)
        {
            builder.AppendLine($"- Parent: [{GetParentDisplayName(command)}]({CreateRelativeLink(relativePath, parentPath)})");
        }

        builder.AppendLine();
        AppendCommandBody(
            command,
            builder,
            includeMetadata,
            headingLevel: 2,
            currentPagePath: relativePath);

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static void AppendInfoSection(OpenCliDocument document, StringBuilder builder)
    {
        if (document.Conventions is not null)
        {
            builder.AppendLine("### Conventions");
            builder.AppendLine();
            builder.AppendLine($"- Group short options: `{document.Conventions.GroupOptions?.ToString() ?? "unspecified"}`");
            builder.AppendLine($"- Option separator: `{document.Conventions.OptionSeparator ?? "unspecified"}`");
            builder.AppendLine();
        }

        if (document.Info.Contact is not null)
        {
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

        if (document.Info.License is not null)
        {
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

        if (document.Examples.Count > 0)
        {
            builder.AppendLine("### Examples");
            builder.AppendLine();
            foreach (var example in document.Examples)
            {
                builder.AppendLine($"- `{example}`");
            }

            builder.AppendLine();
        }

        if (document.ExitCodes.Count > 0)
        {
            builder.AppendLine("### Exit Codes");
            builder.AppendLine();
            AppendExitCodeTable(document.ExitCodes, builder);
        }
    }

    private static void AppendCommandBody(
        NormalizedCommand command,
        StringBuilder builder,
        bool includeMetadata,
        int headingLevel,
        string? currentPagePath)
    {
        if (!string.IsNullOrWhiteSpace(command.Command.Description))
        {
            builder.AppendLine(command.Command.Description);
            builder.AppendLine();
        }

        var attributes = BuildCommandAttributes(command.Command);
        if (attributes.Count > 0)
        {
            foreach (var attribute in attributes)
            {
                builder.AppendLine($"- {attribute}");
            }

            builder.AppendLine();
        }

        if (command.Commands.Count > 0)
        {
            builder.AppendLine($"{new string('#', headingLevel)} Subcommands");
            builder.AppendLine();
            foreach (var child in command.Commands)
            {
                if (currentPagePath is null)
                {
                    builder.AppendLine($"- `{child.Command.Name}`{FormatDescriptionSuffix(child.Command.Description)}");
                }
                else
                {
                    builder.AppendLine($"- [{child.Command.Name}]({CreateRelativeLink(currentPagePath, GetCommandRelativePath(child))}){FormatDescriptionSuffix(child.Command.Description)}");
                }
            }

            builder.AppendLine();
        }

        if (command.Arguments.Count > 0)
        {
            builder.AppendLine($"{new string('#', headingLevel)} Arguments");
            builder.AppendLine();
            AppendArgumentTable(command.Arguments, builder);
        }

        if (command.DeclaredOptions.Count > 0 || command.InheritedOptions.Count > 0)
        {
            builder.AppendLine($"{new string('#', headingLevel)} Options");
            builder.AppendLine();
            AppendOptionTable(
                command.DeclaredOptions.Select(option => new ResolvedOption
                    {
                        Option = option,
                        IsInherited = false,
                    })
                    .Concat(command.InheritedOptions),
                builder);
        }

        if (command.Command.Examples.Count > 0)
        {
            builder.AppendLine($"{new string('#', headingLevel)} Examples");
            builder.AppendLine();
            foreach (var example in command.Command.Examples)
            {
                builder.AppendLine($"- `{example}`");
            }

            builder.AppendLine();
        }

        if (command.Command.ExitCodes.Count > 0)
        {
            builder.AppendLine($"{new string('#', headingLevel)} Exit Codes");
            builder.AppendLine();
            AppendExitCodeTable(command.Command.ExitCodes, builder);
        }

        if (includeMetadata)
        {
            AppendCommandMetadata(command, builder, headingLevel);
        }
    }

    private static void AppendArgumentTable(IEnumerable<OpenCliArgument> arguments, StringBuilder builder)
    {
        builder.AppendLine("| Name | Required | Arity | Accepted Values | Group | Description |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var argument in arguments)
        {
            builder.Append("| ");
            builder.Append(EscapeCell(argument.Hidden ? $"{argument.Name} (hidden)" : argument.Name));
            builder.Append(" | ");
            builder.Append(argument.Required ? "Yes" : "No");
            builder.Append(" | ");
            builder.Append(EscapeCell(FormatArity(argument)));
            builder.Append(" | ");
            builder.Append(EscapeCell(argument.AcceptedValues.Count == 0 ? "—" : string.Join(", ", argument.AcceptedValues)));
            builder.Append(" | ");
            builder.Append(EscapeCell(argument.Group ?? "—"));
            builder.Append(" | ");
            builder.Append(EscapeCell(argument.Description ?? "—"));
            builder.AppendLine(" |");
        }

        builder.AppendLine();
    }

    private static void AppendOptionTable(IEnumerable<ResolvedOption> options, StringBuilder builder)
    {
        builder.AppendLine("| Name | Aliases | Value | Required | Recursive | Scope | Group | Description |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var resolved in options)
        {
            var option = resolved.Option;
            builder.Append("| ");
            builder.Append(EscapeCell(option.Hidden ? $"{option.Name} (hidden)" : option.Name));
            builder.Append(" | ");
            builder.Append(EscapeCell(option.Aliases.Count == 0 ? "—" : string.Join(", ", option.Aliases)));
            builder.Append(" | ");
            builder.Append(EscapeCell(FormatOptionValue(option)));
            builder.Append(" | ");
            builder.Append(option.Required ? "Yes" : "No");
            builder.Append(" | ");
            builder.Append(option.Recursive ? "Yes" : "No");
            builder.Append(" | ");
            builder.Append(EscapeCell(resolved.IsInherited ? $"Inherited from {resolved.InheritedFromPath}" : "Declared"));
            builder.Append(" | ");
            builder.Append(EscapeCell(option.Group ?? "—"));
            builder.Append(" | ");
            builder.Append(EscapeCell(option.Description ?? "—"));
            builder.AppendLine(" |");
        }

        builder.AppendLine();
    }

    private static void AppendExitCodeTable(IEnumerable<OpenCliExitCode> exitCodes, StringBuilder builder)
    {
        builder.AppendLine("| Code | Description |");
        builder.AppendLine("| --- | --- |");
        foreach (var exitCode in exitCodes)
        {
            builder.AppendLine($"| `{exitCode.Code}` | {EscapeCell(exitCode.Description ?? "—")} |");
        }

        builder.AppendLine();
    }

    private static void AppendRootMetadata(OpenCliDocument document, StringBuilder builder)
    {
        if (document.Metadata.Count == 0)
        {
            return;
        }

        builder.AppendLine("## Metadata Appendix");
        builder.AppendLine();
        builder.AppendLine("### Root");
        builder.AppendLine();
        AppendMetadataList(document.Metadata, builder);
    }

    private static void AppendCommandMetadata(NormalizedCommand command, StringBuilder builder, int headingLevel)
    {
        var hasMetadata = command.Command.Metadata.Count > 0 ||
                          command.Arguments.Any(argument => argument.Metadata.Count > 0) ||
                          command.DeclaredOptions.Any(option => option.Metadata.Count > 0) ||
                          command.InheritedOptions.Any(option => option.Option.Metadata.Count > 0);

        if (!hasMetadata)
        {
            return;
        }

        builder.AppendLine($"{new string('#', headingLevel)} Metadata Appendix");
        builder.AppendLine();

        if (command.Command.Metadata.Count > 0)
        {
            builder.AppendLine("#### Command");
            builder.AppendLine();
            AppendMetadataList(command.Command.Metadata, builder);
        }

        var argumentMetadata = command.Arguments.Where(argument => argument.Metadata.Count > 0).ToList();
        if (argumentMetadata.Count > 0)
        {
            builder.AppendLine("#### Arguments");
            builder.AppendLine();
            foreach (var argument in argumentMetadata)
            {
                builder.AppendLine($"- `{argument.Name}`");
                AppendMetadataList(argument.Metadata, builder, "  ");
            }

            builder.AppendLine();
        }

        var declaredMetadata = command.DeclaredOptions.Where(option => option.Metadata.Count > 0).ToList();
        if (declaredMetadata.Count > 0)
        {
            builder.AppendLine("#### Declared Options");
            builder.AppendLine();
            foreach (var option in declaredMetadata)
            {
                builder.AppendLine($"- `{option.Name}`");
                AppendMetadataList(option.Metadata, builder, "  ");
            }

            builder.AppendLine();
        }

        var inheritedMetadata = command.InheritedOptions.Where(option => option.Option.Metadata.Count > 0).ToList();
        if (inheritedMetadata.Count > 0)
        {
            builder.AppendLine("#### Inherited Options");
            builder.AppendLine();
            foreach (var option in inheritedMetadata)
            {
                builder.AppendLine($"- `{option.Option.Name}` from `{option.InheritedFromPath}`");
                AppendMetadataList(option.Option.Metadata, builder, "  ");
            }

            builder.AppendLine();
        }
    }

    private static void AppendMetadataList(IEnumerable<OpenCliMetadata> metadata, StringBuilder builder, string indent = "")
    {
        foreach (var item in metadata)
        {
            builder.Append(indent);
            builder.Append("- `");
            builder.Append(item.Name);
            builder.Append("`: ");
            builder.AppendLine(FormatMetadataValue(item));
        }

        if (string.IsNullOrEmpty(indent))
        {
            builder.AppendLine();
        }
    }

    private static string FormatMetadataValue(OpenCliMetadata metadata)
    {
        if (metadata.Value is null)
        {
            return "null";
        }

        return metadata.Value.GetValueKind() switch
        {
            JsonValueKind.String => $"`{metadata.Value.GetValue<string>()}`",
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => $"`{metadata.Value}`",
            _ => $"```json{Environment.NewLine}{metadata.Value.ToJsonString(new JsonSerializerOptions { WriteIndented = true })}{Environment.NewLine}```",
        };
    }

    private static string FormatArity(OpenCliArgument argument)
    {
        var minimum = argument.Arity?.Minimum ?? (argument.Required ? 1 : 0);
        var maximum = argument.Arity?.Maximum;
        return maximum switch
        {
            null => $"{minimum}..n",
            _ when minimum == maximum => minimum.ToString(),
            _ => $"{minimum}..{maximum}",
        };
    }

    private static string FormatOptionValue(OpenCliOption option)
    {
        if (option.Arguments.Count == 0)
        {
            return "flag";
        }

        return string.Join(' ', option.Arguments.Select(argument =>
        {
            var maximum = argument.Arity?.Maximum;
            return maximum is null or > 1
                ? $"<{argument.Name}...>"
                : $"<{argument.Name}>";
        }));
    }

    private static List<string> BuildCommandAttributes(OpenCliCommand command)
    {
        var attributes = new List<string>();
        if (command.Aliases.Count > 0)
        {
            attributes.Add($"Aliases: `{string.Join("`, `", command.Aliases)}`");
        }

        if (command.Interactive)
        {
            attributes.Add("Interactive command");
        }

        if (command.Hidden)
        {
            attributes.Add("Hidden command");
        }

        return attributes;
    }

    private static string EscapeCell(string value)
    {
        return value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static string CreateAnchorId(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (character is ' ' or '-' or '_')
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string GetCommandRelativePath(NormalizedCommand command)
    {
        var parts = command.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizePathSegment)
            .ToArray();

        if (command.Commands.Count > 0)
        {
            return Path.Combine(parts).Replace('\\', '/') + "/index.md";
        }

        var parent = parts.Length > 1 ? Path.Combine(parts[..^1]).Replace('\\', '/') : string.Empty;
        var fileName = parts[^1] + ".md";
        return string.IsNullOrEmpty(parent) ? fileName : $"{parent}/{fileName}";
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalid.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "command" : sanitized;
    }

    private static string CreateRelativeLink(string currentPagePath, string targetPath)
    {
        var currentDirectory = Path.GetDirectoryName(currentPagePath)?.Replace('\\', '/');
        var baseDirectory = string.IsNullOrWhiteSpace(currentDirectory) ? "." : currentDirectory;
        return Path.GetRelativePath(baseDirectory, targetPath).Replace('\\', '/');
    }

    private static string? GetParentRelativePath(NormalizedCommand command)
    {
        var parts = command.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return "index.md";
        }

        var parentParts = parts[..^1];
        var leafParent = parentParts.Length == 1
            ? string.Empty
            : Path.Combine(parentParts[..^1].Select(SanitizePathSegment).ToArray()).Replace('\\', '/');
        var lastParent = SanitizePathSegment(parentParts[^1]);
        return string.IsNullOrEmpty(leafParent)
            ? $"{lastParent}/index.md"
            : $"{leafParent}/{lastParent}/index.md";
    }

    private static string GetParentDisplayName(NormalizedCommand command)
    {
        var parts = command.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts[..^1]);
    }

    private static string FormatDescriptionSuffix(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? string.Empty : $" — {description}";
    }
}

public sealed record RelativeMarkdownFile(string RelativePath, string Content);
