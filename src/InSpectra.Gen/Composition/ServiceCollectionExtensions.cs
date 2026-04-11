using InSpectra.Gen.Acquisition.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace InSpectra.Gen.Composition;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInSpectraGen(this IServiceCollection services)
    {
        services.AddOpenCliServices();
        services.AddTargetServices();
        services.AddRenderingServices();
        services.AddInSpectraAcquisition();
        services.Configure<ViewerBundleLocatorOptions>(_ => { });

        return services;
    }

    private static IServiceCollection AddOpenCliServices(this IServiceCollection services)
    {
        services.AddSingleton<OpenCliSchemaProvider>();
        services.AddSingleton<OpenCliDocumentLoader>();
        services.AddSingleton<OpenCliDocumentCloner>();
        services.AddSingleton<OpenCliDocumentSerializer>();
        services.AddSingleton<OpenCliXmlEnricher>();
        services.AddSingleton<OpenCliNormalizer>();
        services.AddSingleton<OpenCliNativeAcquisitionSupport>();
        services.AddSingleton<IOpenCliAcquisitionService, OpenCliAcquisitionService>();
        services.AddSingleton<IOpenCliGenerationService, OpenCliGenerationService>();
        services.AddSingleton<AcquisitionAnalyzerService>();

        return services;
    }

    private static IServiceCollection AddTargetServices(this IServiceCollection services)
    {
        services.AddSingleton<ExecutableResolver>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<LocalCliFrameworkDetector>();
        services.AddSingleton<LocalCliTargetFactory>();
        services.AddSingleton<PackageCliTargetFactory>();
        services.AddSingleton<DotnetBuildOutputResolver>();

        return services;
    }

    private static IServiceCollection AddRenderingServices(this IServiceCollection services)
    {
        services.AddSingleton<RenderStatsFactory>();
        services.AddSingleton<RenderModelFormatter>();
        services.AddSingleton<OverviewFormatter>();
        services.AddSingleton<CommandPathResolver>();
        services.AddSingleton<MarkdownTableRenderer>();
        services.AddSingleton<MarkdownMetadataRenderer>();
        services.AddSingleton<MarkdownSectionRenderer>();
        services.AddSingleton<MarkdownRenderer>();
        services.AddSingleton<IDocumentRenderService, DocumentRenderService>();
        services.AddSingleton<IViewerBundleLocator, ViewerBundleLocator>();
        services.AddSingleton<MarkdownRenderService>();
        services.AddSingleton<HtmlRenderService>();

        return services;
    }
}
