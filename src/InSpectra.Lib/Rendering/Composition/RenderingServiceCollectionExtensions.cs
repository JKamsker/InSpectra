using InSpectra.Lib.Rendering.Contracts;
using Microsoft.Extensions.DependencyInjection;
using InSpectra.Lib.Rendering.Html;
using InSpectra.Lib.Rendering.Html.Bundle;
using InSpectra.Lib.Rendering.Markdown;
using InSpectra.Lib.Rendering.Pipeline;

namespace InSpectra.Lib.Rendering.Composition;

/// <summary>
/// Public composition entry point for the rendering logical module inside
/// <c>InSpectra.Gen</c>. Registers the render model formatters, Markdown and
/// HTML renderers, and viewer bundle locator the generation pipeline needs.
/// </summary>
internal static class RenderingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the rendering, Markdown, HTML, and viewer bundle services into
    /// the provided service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection for chaining.</returns>
    internal static IServiceCollection AddInSpectraRendering(this IServiceCollection services)
    {
        services.Configure<ViewerBundleLocatorOptions>(_ => { });
        services.AddSingleton<OpenCliNormalizer>();
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
        services.AddSingleton<IMarkdownRenderService, MarkdownRenderService>();
        services.AddSingleton<IHtmlRenderService, HtmlRenderService>();

        return services;
    }
}
