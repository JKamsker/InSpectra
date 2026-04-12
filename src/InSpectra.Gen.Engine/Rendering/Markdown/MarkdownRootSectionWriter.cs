using System.Text;
using InSpectra.Gen.Engine.Rendering.Pipeline.Model;
using InSpectra.Gen.Engine.Rendering.Contracts;
using InSpectra.Gen.Engine.Rendering.Pipeline;

namespace InSpectra.Gen.Engine.Rendering.Markdown;

internal sealed class MarkdownRootSectionWriter(
    MarkdownSectionRenderer sectionRenderer,
    MarkdownTableRenderer tableRenderer,
    CommandPathResolver pathResolver,
    RenderModelFormatter formatter,
    OverviewFormatter overviewFormatter)
{
    public void AppendHeader(
        NormalizedCliDocument document,
        StringBuilder builder,
        MarkdownRenderOptions? markdownOptions)
    {
        builder.AppendLine($"# {ResolveTitle(document, markdownOptions)}");
        builder.AppendLine();
        builder.AppendLine($"- Version: `{document.Source.Info.Version}`");
        builder.AppendLine($"- OpenCLI: `{document.Source.OpenCliVersion}`");

        var summary = overviewFormatter.BuildSummary(document, ResolveTitle(document, markdownOptions));
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

    public void AppendOverview(
        NormalizedCliDocument document,
        StringBuilder builder,
        string? currentPagePath,
        HybridLinkContext? hybridContext = null,
        MarkdownRenderOptions? markdownOptions = null)
    {
        builder.AppendLine();
        builder.AppendLine("<a id=\"overview\"></a>");
        builder.AppendLine("## Overview");
        builder.AppendLine();
        sectionRenderer.AppendInfoSection(document.Source, builder, markdownOptions?.CommandPrefix);
        AppendOverviewFacts(document, builder);
        AppendAvailableCommands(document.Commands, builder, currentPagePath, hybridContext);
    }

    public void AppendRootSections(
        NormalizedCliDocument document,
        StringBuilder builder,
        bool includeAnchors)
    {
        AppendRootArguments(document, builder, includeAnchors);
        AppendRootOptions(document, builder, includeAnchors);
    }

    public string ResolveTitle(NormalizedCliDocument document, MarkdownRenderOptions? markdownOptions)
    {
        return string.IsNullOrWhiteSpace(markdownOptions?.Title)
            ? document.Source.Info.Title
            : markdownOptions.Title;
    }

    private void AppendRootArguments(
        NormalizedCliDocument document,
        StringBuilder builder,
        bool includeAnchor)
    {
        if (document.RootArguments.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        if (includeAnchor)
        {
            builder.AppendLine("<a id=\"root-arguments\"></a>");
        }

        builder.AppendLine("## Root Arguments");
        builder.AppendLine();
        tableRenderer.AppendArgumentTable(document.RootArguments, builder);
    }

    private void AppendRootOptions(
        NormalizedCliDocument document,
        StringBuilder builder,
        bool includeAnchor)
    {
        if (document.RootOptions.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        if (includeAnchor)
        {
            builder.AppendLine("<a id=\"root-options\"></a>");
        }

        builder.AppendLine("## Root Options");
        builder.AppendLine();
        tableRenderer.AppendOptionTable(
            document.RootOptions.Select(option => new ResolvedOption
            {
                Option = option,
                IsInherited = false,
            }),
            builder);
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
        HybridLinkContext? hybridContext)
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
            var target = ResolveCommandLink(command, currentPagePath, hybridContext);
            builder.AppendLine($"- [{command.Command.Name}]({target}){formatter.FormatDescriptionSuffix(command.Command.Description)}");
        }

        builder.AppendLine();
    }

    private string ResolveCommandLink(
        NormalizedCommand command,
        string? currentPagePath,
        HybridLinkContext? hybridContext)
    {
        if (hybridContext is not null)
        {
            return hybridContext.ResolveTarget(command);
        }

        if (currentPagePath is null)
        {
            return $"#command-{pathResolver.CreateAnchorId(command.Path)}";
        }

        return pathResolver.CreateRelativeLink(
            currentPagePath,
            pathResolver.GetCommandRelativePath(command, "md"));
    }
}
