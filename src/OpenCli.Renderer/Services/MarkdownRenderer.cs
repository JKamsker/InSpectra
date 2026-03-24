using System.Text;
using OpenCli.Renderer.Models;
using OpenCli.Renderer.Runtime;

namespace OpenCli.Renderer.Services;

public sealed class MarkdownRenderer(
    MarkdownSectionRenderer sectionRenderer,
    MarkdownTableRenderer tableRenderer,
    MarkdownMetadataRenderer metadataRenderer,
    CommandPathResolver pathResolver,
    RenderModelFormatter formatter) : IDocumentRenderer
{
    public DocumentFormat Format => DocumentFormat.Markdown;

    public string RenderSingle(NormalizedCliDocument document, bool includeMetadata)
    {
        var builder = new StringBuilder();
        AppendHeader(document, builder);
        AppendTableOfContents(document, builder);
        AppendOverview(document, builder);
        AppendRootArguments(document, builder);
        AppendRootOptions(document, builder);
        AppendCommandSections(document.Commands, builder, includeMetadata, 2);

        if (includeMetadata)
        {
            metadataRenderer.AppendRootMetadata(document, builder);
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    public IReadOnlyList<RelativeRenderedFile> RenderTree(NormalizedCliDocument document, bool includeMetadata)
    {
        var files = new List<RelativeRenderedFile>
        {
            new("index.md", RenderRootPage(document, includeMetadata)),
        };

        foreach (var command in document.Commands)
        {
            AppendCommandPages(command, includeMetadata, files);
        }

        return files;
    }

    private void AppendHeader(NormalizedCliDocument document, StringBuilder builder)
    {
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
    }

    private void AppendTableOfContents(NormalizedCliDocument document, StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("## Table of Contents");
        builder.AppendLine();
        builder.AppendLine("- [Overview](#overview)");
        if (document.RootArguments.Count > 0) builder.AppendLine("- [Root Arguments](#root-arguments)");
        if (document.RootOptions.Count > 0) builder.AppendLine("- [Root Options](#root-options)");
        if (document.Commands.Count == 0) return;

        builder.AppendLine("- [Commands](#commands)");
        AppendCommandToc(document.Commands, builder, 1);
    }

    private void AppendOverview(NormalizedCliDocument document, StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("<a id=\"overview\"></a>");
        builder.AppendLine("## Overview");
        builder.AppendLine();
        sectionRenderer.AppendInfoSection(document.Source, builder);
    }

    private void AppendRootArguments(NormalizedCliDocument document, StringBuilder builder)
    {
        if (document.RootArguments.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("<a id=\"root-arguments\"></a>");
        builder.AppendLine("## Root Arguments");
        builder.AppendLine();
        tableRenderer.AppendArgumentTable(document.RootArguments, builder);
    }

    private void AppendRootOptions(NormalizedCliDocument document, StringBuilder builder)
    {
        if (document.RootOptions.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("<a id=\"root-options\"></a>");
        builder.AppendLine("## Root Options");
        builder.AppendLine();
        tableRenderer.AppendOptionTable(document.RootOptions.Select(option => new ResolvedOption { Option = option, IsInherited = false }), builder);
    }

    private void AppendCommandToc(IEnumerable<NormalizedCommand> commands, StringBuilder builder, int depth)
    {
        var prefix = new string(' ', depth * 2);
        foreach (var command in commands)
        {
            builder.AppendLine($"{prefix}- [{command.Path}](#command-{pathResolver.CreateAnchorId(command.Path)})");
            AppendCommandToc(command.Commands, builder, depth + 1);
        }
    }

    private void AppendCommandSections(IEnumerable<NormalizedCommand> commands, StringBuilder builder, bool includeMetadata, int headingLevel)
    {
        if (!commands.Any())
        {
            return;
        }

        if (headingLevel == 2)
        {
            builder.AppendLine();
            builder.AppendLine("<a id=\"commands\"></a>");
            builder.AppendLine("## Commands");
            builder.AppendLine();
        }

        foreach (var command in commands)
        {
            builder.AppendLine($"<a id=\"command-{pathResolver.CreateAnchorId(command.Path)}\"></a>");
            builder.AppendLine($"{new string('#', headingLevel)} `{command.Path}`");
            builder.AppendLine();
            sectionRenderer.AppendCommandBody(command, builder, includeMetadata, headingLevel + 1, null);
            AppendCommandSections(command.Commands, builder, includeMetadata, headingLevel + 1);
        }
    }

    private string RenderRootPage(NormalizedCliDocument document, bool includeMetadata)
    {
        var builder = new StringBuilder();
        AppendHeader(document, builder);
        builder.AppendLine();
        sectionRenderer.AppendInfoSection(document.Source, builder);
        if (document.RootArguments.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Root Arguments");
            builder.AppendLine();
            tableRenderer.AppendArgumentTable(document.RootArguments, builder);
        }

        if (document.RootOptions.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Root Options");
            builder.AppendLine();
            tableRenderer.AppendOptionTable(document.RootOptions.Select(option => new ResolvedOption { Option = option, IsInherited = false }), builder);
        }

        builder.AppendLine();
        builder.AppendLine("## Commands");
        builder.AppendLine();
        foreach (var command in document.Commands)
        {
            builder.AppendLine($"- [{command.Command.Name}]({pathResolver.GetCommandRelativePath(command, "md")}){formatter.FormatDescriptionSuffix(command.Command.Description)}");
        }

        if (includeMetadata)
        {
            metadataRenderer.AppendRootMetadata(document, builder);
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private void AppendCommandPages(NormalizedCommand command, bool includeMetadata, ICollection<RelativeRenderedFile> files)
    {
        var relativePath = pathResolver.GetCommandRelativePath(command, "md");
        files.Add(new RelativeRenderedFile(relativePath, RenderCommandPage(command, includeMetadata, relativePath)));

        foreach (var child in command.Commands)
        {
            AppendCommandPages(child, includeMetadata, files);
        }
    }

    private string RenderCommandPage(NormalizedCommand command, bool includeMetadata, string relativePath)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# `{command.Path}`");
        builder.AppendLine();
        builder.AppendLine($"- Root: [index]({pathResolver.CreateRelativeLink(relativePath, "index.md")})");

        var parentPath = pathResolver.GetParentRelativePath(command, "md");
        if (parentPath is not null)
        {
            builder.AppendLine($"- Parent: [{pathResolver.GetParentDisplayName(command)}]({pathResolver.CreateRelativeLink(relativePath, parentPath)})");
        }

        builder.AppendLine();
        sectionRenderer.AppendCommandBody(command, builder, includeMetadata, 2, relativePath);
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }
}
