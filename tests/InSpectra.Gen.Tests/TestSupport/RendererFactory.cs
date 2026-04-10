using InSpectra.Gen.Services;

namespace InSpectra.Gen.Tests.TestSupport;

public static class RendererFactory
{
    public static DocumentRenderService CreateDocumentRenderService()
    {
        var schemaProvider = new OpenCliSchemaProvider();
        var documentLoader = new OpenCliDocumentLoader(schemaProvider);
        var processRunner = new ProcessRunner();
        var acquisitionService = new OpenCliAcquisitionService(
            new ExecutableResolver(),
            processRunner,
            new LocalCliTargetFactory(new LocalCliFrameworkDetector()),
            new PackageCliTargetFactory(),
            new DotnetBuildOutputResolver(processRunner),
            new AcquisitionAnalyzerService());
        return new DocumentRenderService(
            documentLoader,
            new OpenCliDocumentCloner(),
            new OpenCliXmlEnricher(),
            acquisitionService);
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
