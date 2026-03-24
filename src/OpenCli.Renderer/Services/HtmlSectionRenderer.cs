using System.Text;
using OpenCli.Renderer.Models;

namespace OpenCli.Renderer.Services;

public sealed class HtmlSectionRenderer(
    HtmlContentFormatter contentFormatter,
    HtmlBlockRenderer blockRenderer,
    RenderModelFormatter formatter,
    CommandPathResolver pathResolver,
    OverviewFormatter overviewFormatter)
{
    public void AppendRootHero(NormalizedCliDocument document, StringBuilder builder)
    {
        builder.AppendLine("<section class=\"panel hero\" id=\"overview\">");
        builder.AppendLine("<span class=\"eyebrow\">OpenCLI Renderer</span>");
        builder.AppendLine($"<h1>{contentFormatter.Encode(document.Source.Info.Title)}</h1>");
        builder.AppendLine("<div class=\"badge-row\">");
        builder.AppendLine($"<span class=\"badge badge-primary\">v{contentFormatter.Encode(document.Source.Info.Version)}</span>");
        builder.AppendLine($"<span class=\"badge\">OpenCLI {contentFormatter.Encode(document.Source.OpenCliVersion)}</span>");
        if (document.Source.Interactive) builder.AppendLine("<span class=\"badge badge-success\">Interactive</span>");
        builder.AppendLine("</div>");
        var summary = overviewFormatter.BuildSummary(document);
        if (!string.IsNullOrWhiteSpace(summary))
        {
            builder.AppendLine($"<p class=\"lede\">{contentFormatter.Encode(summary)}</p>");
        }

        builder.AppendLine(contentFormatter.RenderParagraphBlock(document.Source.Info.Description));
        builder.AppendLine("</section>");
    }

    public void AppendRootDetails(NormalizedCliDocument document, StringBuilder builder, bool includeMetadata)
        => AppendRootDetails(document, builder, includeMetadata, command => $"#command-{pathResolver.CreateAnchorId(command.Path)}");

    public void AppendRootDetails(
        NormalizedCliDocument document,
        StringBuilder builder,
        bool includeMetadata,
        Func<NormalizedCommand, string> commandLinkFactory)
    {
        AppendOverviewCards(document, builder);
        AppendAvailableCommands(document.Commands, builder, commandLinkFactory);
        AppendRootArguments(document, builder);
        AppendRootOptions(document, builder);
        AppendExamples(document.Source.Examples, builder);
        AppendExitCodes(document.Source.ExitCodes, builder);

        if (includeMetadata && document.Source.Metadata.Count > 0)
        {
            builder.AppendLine("<section class=\"panel section\" id=\"root-metadata\"><div class=\"section-head\"><span class=\"eyebrow\">Appendix</span><h2>Metadata</h2></div>");
            blockRenderer.AppendMetadataPanel(document.Source.Metadata, builder);
            builder.AppendLine("</section>");
        }
    }

    public void AppendSinglePageCommandSections(IEnumerable<NormalizedCommand> commands, StringBuilder builder, bool includeMetadata)
    {
        foreach (var command in commands)
        {
            AppendCommandBody(command, builder, includeMetadata, child => $"#command-{pathResolver.CreateAnchorId(child.Path)}", true);
            AppendSinglePageCommandSections(command.Commands, builder, includeMetadata);
        }
    }

    public void AppendCommandBreadcrumb(NormalizedCliDocument document, NormalizedCommand command, StringBuilder builder)
    {
        builder.Append("<nav class=\"breadcrumb\">");
        builder.Append($"<a href=\"#overview\">{contentFormatter.Encode(document.Source.Info.Title)}</a>");

        var segments = command.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length; i++)
        {
            builder.Append("<span class=\"crumb-sep\">›</span>");
            if (i == segments.Length - 1)
            {
                builder.Append($"<span class=\"crumb-current\">{contentFormatter.Encode(segments[i])}</span>");
            }
            else
            {
                var partialPath = string.Join(" ", segments.Take(i + 1));
                builder.Append($"<a href=\"#command-{pathResolver.CreateAnchorId(partialPath)}\">{contentFormatter.Encode(segments[i])}</a>");
            }
        }

        builder.AppendLine("</nav>");
    }

    public void AppendCommandBody(
        NormalizedCommand command,
        StringBuilder builder,
        bool includeMetadata,
        Func<NormalizedCommand, string> childLinkFactory,
        bool includeWrapper)
    {
        if (includeWrapper) builder.AppendLine($"<section class=\"panel command-detail\" id=\"command-{pathResolver.CreateAnchorId(command.Path)}\">");
        builder.AppendLine($"<h2>{contentFormatter.Encode(command.Command.Name)}</h2>");
        builder.AppendLine(contentFormatter.RenderParagraphBlock(command.Command.Description, "No description provided."));
        AppendAttributes(command.Command, builder);
        AppendSubcommands(command, builder, childLinkFactory);
        AppendArguments(command, builder);
        AppendOptions(command, builder);
        AppendCommandExamples(command.Command.Examples, builder);
        AppendCommandExitCodes(command.Command.ExitCodes, builder);
        if (includeMetadata) blockRenderer.AppendCommandMetadata(command, builder);
        AppendEmptyState(command, builder);
        if (includeWrapper) builder.AppendLine("</section>");
    }

    private void AppendOverviewCards(NormalizedCliDocument document, StringBuilder builder)
    {
        var cards = new List<string>();
        var facts = overviewFormatter.BuildFacts(document);
        if (facts.Count > 0)
        {
            cards.Add($"<article class=\"panel info-card\"><h3>Reference scope</h3><dl>{string.Concat(facts.Select(fact => contentFormatter.CreateDefinition(fact.Label, fact.Value)))}</dl></article>");
        }

        if (document.Source.Conventions is not null)
        {
            cards.Add($"<article class=\"panel info-card\"><h3>Conventions</h3><dl>{contentFormatter.CreateDefinition("Group short options", document.Source.Conventions.GroupOptions?.ToString() ?? "unspecified")}{contentFormatter.CreateDefinition("Option separator", document.Source.Conventions.OptionSeparator ?? "unspecified")}</dl></article>");
        }

        if (document.Source.Info.Contact is not null)
        {
            cards.Add($"<article class=\"panel info-card\"><h3>Contact</h3><dl>{contentFormatter.CreateDefinition("Name", document.Source.Info.Contact.Name)}{contentFormatter.CreateDefinition("Email", document.Source.Info.Contact.Email)}{contentFormatter.CreateLinkDefinition("URL", document.Source.Info.Contact.Url)}</dl></article>");
        }

        if (document.Source.Info.License is not null)
        {
            cards.Add($"<article class=\"panel info-card\"><h3>License</h3><dl>{contentFormatter.CreateDefinition("Name", document.Source.Info.License.Name)}{contentFormatter.CreateDefinition("Identifier", document.Source.Info.License.Identifier)}{contentFormatter.CreateLinkDefinition("URL", document.Source.Info.License.Url)}</dl></article>");
        }

        if (cards.Count == 0)
        {
            return;
        }

        builder.AppendLine("<section class=\"section\"><div class=\"section-head\"><span class=\"eyebrow\">Overview</span><h2>Reference context</h2></div><div class=\"info-grid\">");
        foreach (var card in cards) builder.AppendLine(card);
        builder.AppendLine("</div></section>");
    }

    private void AppendAvailableCommands(
        IEnumerable<NormalizedCommand> commands,
        StringBuilder builder,
        Func<NormalizedCommand, string> commandLinkFactory)
    {
        var topLevelCommands = commands.ToList();
        if (topLevelCommands.Count == 0)
        {
            return;
        }

        builder.AppendLine("<section class=\"panel section\" id=\"available-commands\"><div class=\"section-head\"><span class=\"eyebrow\">Explore</span><h2>Available commands</h2></div><div class=\"card-grid\">");
        foreach (var command in topLevelCommands)
        {
            builder.AppendLine($"<a class=\"command-card\" href=\"{contentFormatter.Encode(commandLinkFactory(command))}\"><div class=\"command-card-head\"><strong>{contentFormatter.Encode(command.Command.Name)}</strong><span class=\"command-card-arrow\">\u2192</span></div><p>{contentFormatter.EncodeOrFallback(command.Command.Description, "No description provided.")}</p></a>");
        }

        builder.AppendLine("</div></section>");
    }

    private void AppendRootArguments(NormalizedCliDocument document, StringBuilder builder)
    {
        if (document.RootArguments.Count == 0) return;
        builder.AppendLine("<section class=\"panel section\" id=\"root-arguments\"><div class=\"section-head\"><span class=\"eyebrow\">Input</span><h2>Root arguments</h2></div>");
        blockRenderer.AppendArgumentTable(document.RootArguments, builder);
        builder.AppendLine("</section>");
    }

    private void AppendRootOptions(NormalizedCliDocument document, StringBuilder builder)
    {
        if (document.RootOptions.Count == 0) return;
        builder.AppendLine("<section class=\"panel section\" id=\"root-options\"><div class=\"section-head\"><span class=\"eyebrow\">Flags</span><h2>Root options</h2></div>");
        blockRenderer.AppendOptionCards(document.RootOptions.Select(option => new ResolvedOption { Option = option, IsInherited = false }), builder);
        builder.AppendLine("</section>");
    }

    private void AppendExamples(IEnumerable<string> examples, StringBuilder builder)
    {
        var exampleList = examples.ToList();
        if (exampleList.Count == 0) return;
        builder.AppendLine("<section class=\"panel section\" id=\"root-examples\"><div class=\"section-head\"><span class=\"eyebrow\">Usage</span><h2>Examples</h2></div>");
        blockRenderer.AppendExamples(exampleList, builder);
        builder.AppendLine("</section>");
    }

    private void AppendExitCodes(IEnumerable<OpenCliExitCode> exitCodes, StringBuilder builder)
    {
        var codes = exitCodes.ToList();
        if (codes.Count == 0) return;
        builder.AppendLine("<section class=\"panel section\" id=\"root-exit-codes\"><div class=\"section-head\"><span class=\"eyebrow\">Runtime</span><h2>Exit codes</h2></div>");
        blockRenderer.AppendExitCodeTable(codes, builder);
        builder.AppendLine("</section>");
    }

    private void AppendAttributes(OpenCliCommand command, StringBuilder builder)
    {
        var attributes = formatter.BuildCommandAttributes(command);
        if (attributes.Count == 0) return;
        builder.AppendLine("<div class=\"badge-row\">");
        foreach (var attribute in attributes) builder.AppendLine($"<span class=\"badge\">{contentFormatter.Encode(attribute)}</span>");
        builder.AppendLine("</div>");
    }

    private void AppendSubcommands(NormalizedCommand command, StringBuilder builder, Func<NormalizedCommand, string> childLinkFactory)
    {
        if (command.Commands.Count == 0) return;
        builder.AppendLine("<div class=\"detail-block\"><h3><svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"#3b82f6\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z\"/><path d=\"M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z\"/></svg>Subcommands</h3><div class=\"card-grid\">");
        foreach (var child in command.Commands)
        {
            builder.AppendLine($"<a class=\"command-card\" href=\"{contentFormatter.Encode(childLinkFactory(child))}\"><div class=\"command-card-head\"><strong>{contentFormatter.Encode(child.Command.Name)}</strong><span class=\"command-card-arrow\">\u2192</span></div><p>{contentFormatter.EncodeOrFallback(child.Command.Description, "No description provided.")}</p></a>");
        }

        builder.AppendLine("</div></div>");
    }

    private void AppendArguments(NormalizedCommand command, StringBuilder builder)
    {
        if (command.Arguments.Count == 0) return;
        builder.AppendLine("<div class=\"detail-block\"><h3><svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"#f59e0b\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M4 7V4h16v3\"/><path d=\"M9 20h6\"/><path d=\"M12 4v16\"/></svg>Arguments</h3>");
        blockRenderer.AppendArgumentTable(command.Arguments, builder);
        builder.AppendLine("</div>");
    }

    private void AppendOptions(NormalizedCommand command, StringBuilder builder)
    {
        if (command.DeclaredOptions.Count == 0 && command.InheritedOptions.Count == 0) return;
        builder.AppendLine("<div class=\"detail-block\"><h3><svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"#818cf8\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M20 7h-9\"/><path d=\"M14 17H5\"/><circle cx=\"17\" cy=\"17\" r=\"3\"/><circle cx=\"7\" cy=\"7\" r=\"3\"/></svg>Options</h3>");
        blockRenderer.AppendOptionCards(command.DeclaredOptions.Select(option => new ResolvedOption { Option = option, IsInherited = false }).Concat(command.InheritedOptions), builder);
        builder.AppendLine("</div>");
    }

    private void AppendCommandExamples(IEnumerable<string> examples, StringBuilder builder)
    {
        var exampleList = examples.ToList();
        if (exampleList.Count == 0) return;
        builder.AppendLine("<div class=\"detail-block\"><h3><svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"#34d399\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"m4 17 6-5-6-5\"/><path d=\"M12 19h8\"/></svg>Examples</h3>");
        blockRenderer.AppendExamples(exampleList, builder);
        builder.AppendLine("</div>");
    }

    private void AppendCommandExitCodes(IEnumerable<OpenCliExitCode> exitCodes, StringBuilder builder)
    {
        var codes = exitCodes.ToList();
        if (codes.Count == 0) return;
        builder.AppendLine("<div class=\"detail-block\"><h3><svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"#f87171\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M12 8v4\"/><path d=\"M12 16h.01\"/></svg>Exit codes</h3>");
        blockRenderer.AppendExitCodeTable(codes, builder);
        builder.AppendLine("</div>");
    }

    private void AppendEmptyState(NormalizedCommand command, StringBuilder builder)
    {
        if (command.Commands.Count > 0 || command.Arguments.Count > 0 ||
            command.DeclaredOptions.Count > 0 || command.InheritedOptions.Count > 0 ||
            command.Command.Examples.Count > 0 || command.Command.ExitCodes.Count > 0)
        {
            return;
        }

        builder.AppendLine("<div class=\"empty-state\"><svg width=\"40\" height=\"40\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"m4 17 6-5-6-5\"/><path d=\"M12 19h8\"/></svg><p>No additional details or options defined for this command.</p></div>");
    }
}
