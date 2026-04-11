using InSpectra.Gen.Rendering.Html;
using InSpectra.Gen.Rendering.Markdown;
using InSpectra.Gen.Viewer;
using Microsoft.Extensions.DependencyInjection;

namespace InSpectra.Gen.Rendering.Composition;

/// <summary>
/// Public composition entry point for the rendering logical module inside
/// <c>InSpectra.Gen</c>. Registers the render model formatters, Markdown and
/// HTML renderers, and viewer bundle locator the generation pipeline needs.
/// </summary>
public static class RenderingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the rendering, Markdown, HTML, and viewer bundle services into
    /// the provided service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInSpectraRendering(this IServiceCollection services)
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
