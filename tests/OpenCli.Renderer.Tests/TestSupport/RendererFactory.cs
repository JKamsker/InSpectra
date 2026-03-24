using OpenCli.Renderer.Services;

namespace OpenCli.Renderer.Tests.TestSupport;

public static class RendererFactory
{
    public static MarkdownRenderer CreateMarkdownRenderer()
    {
        var formatter = new RenderModelFormatter();
        var overviewFormatter = new OverviewFormatter();
        var pathResolver = new CommandPathResolver();
        var tableRenderer = new MarkdownTableRenderer(formatter);
        var metadataRenderer = new MarkdownMetadataRenderer();
        var sectionRenderer = new MarkdownSectionRenderer(tableRenderer, metadataRenderer, formatter, pathResolver);
        return new MarkdownRenderer(sectionRenderer, tableRenderer, metadataRenderer, pathResolver, formatter, overviewFormatter);
    }

    public static HtmlRenderer CreateHtmlRenderer()
    {
        var formatter = new RenderModelFormatter();
        var overviewFormatter = new OverviewFormatter();
        var pathResolver = new CommandPathResolver();
        var contentFormatter = new HtmlContentFormatter();
        var blockRenderer = new HtmlBlockRenderer(contentFormatter, formatter);
        var sectionRenderer = new HtmlSectionRenderer(contentFormatter, blockRenderer, formatter, pathResolver, overviewFormatter);
        var shellRenderer = new HtmlShellRenderer(new HtmlAssetProvider(), contentFormatter, pathResolver);
        return new HtmlRenderer(sectionRenderer, shellRenderer, contentFormatter, pathResolver);
    }
}
