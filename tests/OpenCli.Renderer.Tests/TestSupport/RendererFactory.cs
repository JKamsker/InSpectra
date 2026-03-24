using OpenCli.Renderer.Services;

namespace OpenCli.Renderer.Tests.TestSupport;

public static class RendererFactory
{
    public static DocumentRenderService CreateDocumentRenderService()
    {
        return new DocumentRenderService(
            new OpenCliDocumentLoader(new OpenCliSchemaProvider()),
            new OpenCliDocumentCloner(),
            new OpenCliXmlEnricher(),
            new ExecutableResolver(),
            new ProcessRunner());
    }

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
}
