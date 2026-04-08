using System.Text;
using InSpectra.Gen.Models;

namespace InSpectra.Gen.Services;

public sealed class MarkdownRenderer(
    MarkdownSectionRenderer sectionRenderer,
    MarkdownTableRenderer tableRenderer,
    MarkdownMetadataRenderer metadataRenderer,
    CommandPathResolver pathResolver,
    RenderModelFormatter formatter,
    OverviewFormatter overviewFormatter)
{
    public string RenderSingle(NormalizedCliDocument document, bool includeMetadata)
    {
        var builder = new StringBuilder();
        AppendHeader(document, builder);
        AppendTableOfContents(document, builder);
        AppendOverview(document, builder, currentPagePath: null);
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

    public IReadOnlyList<RelativeRenderedFile> RenderHybrid(
        NormalizedCliDocument document,
        bool includeMetadata,
        int splitDepth)
    {
        if (splitDepth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(splitDepth), "splitDepth must be at least 1.");
        }

        const string readmePath = "README.md";
        var rootContext = new HybridLinkContext(splitDepth, readmePath, pathResolver);

        var files = new List<RelativeRenderedFile>
        {
            new(readmePath, RenderHybridReadme(document, includeMetadata, rootContext)),
        };

        foreach (var command in document.Commands)
        {
            AppendHybridGroupFiles(command, includeMetadata, rootContext, files);
        }

        return files;
    }

    private void AppendHeader(NormalizedCliDocument document, StringBuilder builder)
    {
        builder.AppendLine($"# {document.Source.Info.Title}");
        builder.AppendLine();
        builder.AppendLine($"- Version: `{document.Source.Info.Version}`");
        builder.AppendLine($"- OpenCLI: `{document.Source.OpenCliVersion}`");

        var summary = overviewFormatter.BuildSummary(document);
        if (!string.IsNullOrWhiteSpace(summary))
        {
            builder.AppendLine();
            builder.AppendLine(summary);
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

    private void AppendOverview(NormalizedCliDocument document, StringBuilder builder, string? currentPagePath, HybridLinkContext? hybridContext = null)
    {
        builder.AppendLine();
        builder.AppendLine("<a id=\"overview\"></a>");
        builder.AppendLine("## Overview");
        builder.AppendLine();
        sectionRenderer.AppendInfoSection(document.Source, builder);
        AppendOverviewFacts(document, builder);
        AppendAvailableCommands(document.Commands, builder, currentPagePath, hybridContext);
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

    private void AppendCommandSections(IEnumerable<NormalizedCommand> commands, StringBuilder builder, bool includeMetadata, int headingLevel, HybridLinkContext? hybridContext = null)
    {
        if (!commands.Any())
        {
            return;
        }

        if (headingLevel == 2 && hybridContext is null)
        {
            builder.AppendLine();
            builder.AppendLine("<a id=\"commands\"></a>");
            builder.AppendLine("## Commands");
            builder.AppendLine();
        }

        foreach (var command in commands)
        {
            if (hybridContext is not null && hybridContext.HasOwnFile(command))
            {
                // Command lives in its own file — emit a stub reference instead of inlining the body.
                var target = hybridContext.ResolveTarget(command);
                builder.AppendLine($"{new string('#', headingLevel)} `{command.Path}`");
                builder.AppendLine();
                var suffix = formatter.FormatDescriptionSuffix(command.Command.Description);
                var suffixText = string.IsNullOrEmpty(suffix) ? string.Empty : suffix;
                builder.AppendLine($"See [`{command.Path}`]({target}){suffixText}");
                builder.AppendLine();
                continue;
            }

            builder.AppendLine($"<a id=\"command-{pathResolver.CreateAnchorId(command.Path)}\"></a>");
            builder.AppendLine($"{new string('#', headingLevel)} `{command.Path}`");
            builder.AppendLine();
            sectionRenderer.AppendCommandBody(command, builder, includeMetadata, headingLevel + 1, currentPagePath: null, hybridContext);
            AppendCommandSections(command.Commands, builder, includeMetadata, headingLevel + 1, hybridContext);
        }
    }

    private string RenderRootPage(NormalizedCliDocument document, bool includeMetadata)
    {
        var builder = new StringBuilder();
        AppendHeader(document, builder);
        AppendOverview(document, builder, "index.md");
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

        if (includeMetadata)
        {
            metadataRenderer.AppendRootMetadata(document, builder);
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private void AppendOverviewFacts(NormalizedCliDocument document, StringBuilder builder)
    {
        var facts = overviewFormatter.BuildFacts(document);
        if (facts.Count == 0)
        {
            return;
        }

        builder.AppendLine("### CLI Scope");
        builder.AppendLine();
        foreach (var (label, value) in facts)
        {
            builder.AppendLine($"- {label}: `{value}`");
        }

        builder.AppendLine();
    }

    private void AppendAvailableCommands(
        IEnumerable<NormalizedCommand> commands,
        StringBuilder builder,
        string? currentPagePath,
        HybridLinkContext? hybridContext = null)
    {
        var topLevelCommands = commands.ToList();
        if (topLevelCommands.Count == 0)
        {
            return;
        }

        builder.AppendLine("### Available Commands");
        builder.AppendLine();
        foreach (var command in topLevelCommands)
        {
            string target;
            if (hybridContext is not null)
            {
                target = hybridContext.ResolveTarget(command);
            }
            else if (currentPagePath is null)
            {
                target = $"#command-{pathResolver.CreateAnchorId(command.Path)}";
            }
            else
            {
                target = pathResolver.CreateRelativeLink(currentPagePath, pathResolver.GetCommandRelativePath(command, "md"));
            }

            builder.AppendLine($"- [{command.Command.Name}]({target}){formatter.FormatDescriptionSuffix(command.Command.Description)}");
        }

        builder.AppendLine();
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

    private string RenderHybridReadme(
        NormalizedCliDocument document,
        bool includeMetadata,
        HybridLinkContext context)
    {
        var builder = new StringBuilder();
        AppendHeader(document, builder);
        AppendOverview(document, builder, context.CurrentPagePath, context);

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

        if (document.Commands.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Commands");
            builder.AppendLine();
            AppendCommandSections(document.Commands, builder, includeMetadata, 3, context);
        }

        if (includeMetadata)
        {
            metadataRenderer.AppendRootMetadata(document, builder);
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private void AppendHybridGroupFiles(
        NormalizedCommand command,
        bool includeMetadata,
        HybridLinkContext parentContext,
        ICollection<RelativeRenderedFile> files)
    {
        if (!parentContext.HasOwnFile(command))
        {
            return;
        }

        var relativePath = pathResolver.GetCommandRelativePath(command, "md");
        var pageContext = parentContext.ForPage(relativePath);
        files.Add(new RelativeRenderedFile(relativePath, RenderHybridGroupPage(command, includeMetadata, pageContext)));

        foreach (var child in command.Commands)
        {
            AppendHybridGroupFiles(child, includeMetadata, pageContext, files);
        }
    }

    private string RenderHybridGroupPage(
        NormalizedCommand command,
        bool includeMetadata,
        HybridLinkContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# `{command.Path}`");
        builder.AppendLine();

        var rootLink = pathResolver.CreateRelativeLink(context.CurrentPagePath, "README.md");
        builder.AppendLine($"- Root: [README]({rootLink})");

        var parentPath = pathResolver.GetParentRelativePath(command, "md");
        if (parentPath is not null && !string.Equals(parentPath, "index.md", StringComparison.Ordinal))
        {
            builder.AppendLine($"- Parent: [{pathResolver.GetParentDisplayName(command)}]({pathResolver.CreateRelativeLink(context.CurrentPagePath, parentPath)})");
        }

        builder.AppendLine();
        sectionRenderer.AppendCommandBody(command, builder, includeMetadata, 2, currentPagePath: null, context);

        if (command.Commands.Count > 0)
        {
            AppendCommandSections(command.Commands, builder, includeMetadata, 2, context);
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }
}
