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
    {
        AppendOverviewCards(document, builder);
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

    public void AppendCommandBody(
        NormalizedCommand command,
        StringBuilder builder,
        bool includeMetadata,
        Func<NormalizedCommand, string> childLinkFactory,
        bool includeWrapper)
    {
        if (includeWrapper) builder.AppendLine($"<section class=\"panel command-detail\" id=\"command-{pathResolver.CreateAnchorId(command.Path)}\">");
        builder.AppendLine($"<span class=\"command-path\">{contentFormatter.Encode(command.Path)}</span>");
        builder.AppendLine($"<h2>{contentFormatter.Encode(command.Command.Name)}</h2>");
        builder.AppendLine(contentFormatter.RenderParagraphBlock(command.Command.Description, "No description provided."));
        AppendAttributes(command.Command, builder);
        AppendSubcommands(command, builder, childLinkFactory);
        AppendArguments(command, builder);
        AppendOptions(command, builder);
        AppendCommandExamples(command.Command.Examples, builder);
        AppendCommandExitCodes(command.Command.ExitCodes, builder);
        if (includeMetadata) blockRenderer.AppendCommandMetadata(command, builder);
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
        builder.AppendLine("<div class=\"detail-block\"><h3>Subcommands</h3><div class=\"card-grid\">");
        foreach (var child in command.Commands)
        {
            builder.AppendLine($"<a class=\"command-card\" href=\"{contentFormatter.Encode(childLinkFactory(child))}\"><strong>{contentFormatter.Encode(child.Command.Name)}</strong><p>{contentFormatter.EncodeOrFallback(child.Command.Description, "No description provided.")}</p></a>");
        }

        builder.AppendLine("</div></div>");
    }

    private void AppendArguments(NormalizedCommand command, StringBuilder builder)
    {
        if (command.Arguments.Count == 0) return;
        builder.AppendLine("<div class=\"detail-block\"><h3>Arguments</h3>");
        blockRenderer.AppendArgumentTable(command.Arguments, builder);
        builder.AppendLine("</div>");
    }

    private void AppendOptions(NormalizedCommand command, StringBuilder builder)
    {
        if (command.DeclaredOptions.Count == 0 && command.InheritedOptions.Count == 0) return;
        builder.AppendLine("<div class=\"detail-block\"><h3>Options</h3>");
        blockRenderer.AppendOptionCards(command.DeclaredOptions.Select(option => new ResolvedOption { Option = option, IsInherited = false }).Concat(command.InheritedOptions), builder);
        builder.AppendLine("</div>");
    }

    private void AppendCommandExamples(IEnumerable<string> examples, StringBuilder builder)
    {
        var exampleList = examples.ToList();
        if (exampleList.Count == 0) return;
        builder.AppendLine("<div class=\"detail-block\"><h3>Examples</h3>");
        blockRenderer.AppendExamples(exampleList, builder);
        builder.AppendLine("</div>");
    }

    private void AppendCommandExitCodes(IEnumerable<OpenCliExitCode> exitCodes, StringBuilder builder)
    {
        var codes = exitCodes.ToList();
        if (codes.Count == 0) return;
        builder.AppendLine("<div class=\"detail-block\"><h3>Exit codes</h3>");
        blockRenderer.AppendExitCodeTable(codes, builder);
        builder.AppendLine("</div>");
    }
}
