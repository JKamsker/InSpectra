using System.Text;
using InSpectra.Gen.Engine.Rendering.Pipeline.Model;
using InSpectra.Gen.Engine.Rendering.Contracts;
using InSpectra.Gen.Engine.Rendering.Pipeline;

namespace InSpectra.Gen.Engine.Rendering.Markdown;

internal sealed class MarkdownRenderer(
    MarkdownSectionRenderer sectionRenderer,
    MarkdownTableRenderer tableRenderer,
    MarkdownMetadataRenderer metadataRenderer,
    CommandPathResolver pathResolver,
    RenderModelFormatter formatter,
    OverviewFormatter overviewFormatter)
{
    private readonly MarkdownRootSectionWriter _rootSectionWriter =
        new(sectionRenderer, tableRenderer, pathResolver, formatter, overviewFormatter);

    public string RenderSingle(
        NormalizedCliDocument document,
        bool includeMetadata,
        MarkdownRenderOptions? markdownOptions = null)
    {
        var builder = new StringBuilder();
        _rootSectionWriter.AppendHeader(document, builder, markdownOptions);
        AppendTableOfContents(document, builder);
        _rootSectionWriter.AppendOverview(document, builder, currentPagePath: null, markdownOptions: markdownOptions);
        _rootSectionWriter.AppendRootSections(document, builder, includeAnchors: true);
        AppendCommandSections(document.Commands, builder, includeMetadata, 2, markdownOptions: markdownOptions);

        if (includeMetadata)
        {
            metadataRenderer.AppendRootMetadata(document, builder);
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    public IReadOnlyList<RelativeRenderedFile> RenderTree(
        NormalizedCliDocument document,
        bool includeMetadata,
        MarkdownRenderOptions? markdownOptions = null)
    {
        var files = new List<RelativeRenderedFile>
        {
            new("index.md", RenderRootPage(document, includeMetadata, markdownOptions)),
        };

        foreach (var command in document.Commands)
        {
            AppendCommandPages(command, includeMetadata, files, markdownOptions);
        }

        return files;
    }

    public IReadOnlyList<RelativeRenderedFile> RenderHybrid(
        NormalizedCliDocument document,
        bool includeMetadata,
        int splitDepth,
        MarkdownRenderOptions? markdownOptions = null)
    {
        if (splitDepth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(splitDepth), "splitDepth must be at least 1.");
        }

        const string readmePath = "README.md";
        var rootContext = new HybridLinkContext(splitDepth, readmePath, pathResolver);

        var files = new List<RelativeRenderedFile>
        {
            new(readmePath, RenderHybridReadme(document, includeMetadata, rootContext, markdownOptions)),
        };

        foreach (var command in document.Commands)
        {
            AppendHybridGroupFiles(command, includeMetadata, rootContext, files, markdownOptions);
        }

        return files;
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

    private void AppendCommandToc(IEnumerable<NormalizedCommand> commands, StringBuilder builder, int depth)
    {
        var prefix = new string(' ', depth * 2);
        foreach (var command in commands)
        {
            builder.AppendLine($"{prefix}- [{command.Path}](#command-{pathResolver.CreateAnchorId(command.Path)})");
            AppendCommandToc(command.Commands, builder, depth + 1);
        }
    }

    private void AppendCommandSections(
        IReadOnlyList<NormalizedCommand> commands,
        StringBuilder builder,
        bool includeMetadata,
        int headingLevel,
        HybridLinkContext? hybridContext = null,
        MarkdownRenderOptions? markdownOptions = null)
    {
        if (commands.Count == 0)
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
                // File-split children are reachable through the Subcommands list. Emitting a stub
                // section here would duplicate that link with no extra information, so we skip it.
                continue;
            }

            builder.AppendLine($"<a id=\"command-{pathResolver.CreateAnchorId(command.Path)}\"></a>");
            builder.AppendLine($"{new string('#', headingLevel)} `{command.Path}`");
            builder.AppendLine();
            sectionRenderer.AppendCommandBody(command, builder, includeMetadata, headingLevel + 1, currentPagePath: null, hybridContext, markdownOptions?.CommandPrefix);
            AppendCommandSections(command.Commands, builder, includeMetadata, headingLevel + 1, hybridContext, markdownOptions);
        }
    }

    private string RenderRootPage(
        NormalizedCliDocument document,
        bool includeMetadata,
        MarkdownRenderOptions? markdownOptions)
    {
        var builder = new StringBuilder();
        _rootSectionWriter.AppendHeader(document, builder, markdownOptions);
        _rootSectionWriter.AppendOverview(document, builder, "index.md", markdownOptions: markdownOptions);
        _rootSectionWriter.AppendRootSections(document, builder, includeAnchors: false);

        if (includeMetadata)
        {
            metadataRenderer.AppendRootMetadata(document, builder);
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private void AppendCommandPages(
        NormalizedCommand command,
        bool includeMetadata,
        ICollection<RelativeRenderedFile> files,
        MarkdownRenderOptions? markdownOptions)
    {
        var relativePath = pathResolver.GetCommandRelativePath(command, "md");
        files.Add(new RelativeRenderedFile(relativePath, RenderCommandPage(command, includeMetadata, relativePath, markdownOptions)));

        foreach (var child in command.Commands)
        {
            AppendCommandPages(child, includeMetadata, files, markdownOptions);
        }
    }

    private string RenderCommandPage(
        NormalizedCommand command,
        bool includeMetadata,
        string relativePath,
        MarkdownRenderOptions? markdownOptions)
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
        sectionRenderer.AppendCommandBody(command, builder, includeMetadata, 2, relativePath, commandPrefix: markdownOptions?.CommandPrefix);
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderHybridReadme(
        NormalizedCliDocument document,
        bool includeMetadata,
        HybridLinkContext context,
        MarkdownRenderOptions? markdownOptions)
    {
        var builder = new StringBuilder();
        _rootSectionWriter.AppendHeader(document, builder, markdownOptions);
        _rootSectionWriter.AppendOverview(document, builder, context.CurrentPagePath, context, markdownOptions);
        _rootSectionWriter.AppendRootSections(document, builder, includeAnchors: false);

        var inlinedTopLevel = document.Commands.Where(c => !context.HasOwnFile(c)).ToList();
        if (inlinedTopLevel.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Commands");
            builder.AppendLine();
            AppendCommandSections(inlinedTopLevel, builder, includeMetadata, 3, context, markdownOptions);
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
        ICollection<RelativeRenderedFile> files,
        MarkdownRenderOptions? markdownOptions)
    {
        if (!parentContext.HasOwnFile(command))
        {
            return;
        }

        var relativePath = pathResolver.GetCommandRelativePath(command, "md");
        var pageContext = parentContext.ForPage(relativePath);
        files.Add(new RelativeRenderedFile(relativePath, RenderHybridGroupPage(command, includeMetadata, pageContext, markdownOptions)));

        foreach (var child in command.Commands)
        {
            AppendHybridGroupFiles(child, includeMetadata, pageContext, files, markdownOptions);
        }
    }

    private string RenderHybridGroupPage(
        NormalizedCommand command,
        bool includeMetadata,
        HybridLinkContext context,
        MarkdownRenderOptions? markdownOptions)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# `{command.Path}`");
        builder.AppendLine();
        builder.AppendLine(BuildHybridBreadcrumb(command, context));
        builder.AppendLine();

        sectionRenderer.AppendCommandBody(command, builder, includeMetadata, 2, currentPagePath: null, context, markdownOptions?.CommandPrefix);

        if (command.Commands.Count > 0)
        {
            AppendCommandSections(command.Commands, builder, includeMetadata, 2, context, markdownOptions);
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string BuildHybridBreadcrumb(NormalizedCommand command, HybridLinkContext context)
    {
        var parts = command.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var segments = new List<string>
        {
            $"[README]({pathResolver.CreateRelativeLink(context.CurrentPagePath, "README.md")})",
        };

        for (var i = 1; i < parts.Length; i++)
        {
            var ancestorFile = pathResolver.BuildGroupFilePath(parts, i, "md");
            var link = pathResolver.CreateRelativeLink(context.CurrentPagePath, ancestorFile);
            segments.Add($"[{parts[i - 1]}]({link})");
        }

        segments.Add($"`{parts[^1]}`");
        return string.Join(" › ", segments);
    }
}
