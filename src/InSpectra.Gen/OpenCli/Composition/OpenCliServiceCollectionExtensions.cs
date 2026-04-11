using InSpectra.Gen.OpenCli.Enrichment;
using InSpectra.Gen.OpenCli.Schema;
using InSpectra.Gen.OpenCli.Serialization;
using InSpectra.Gen.UseCases.Generate;
using Microsoft.Extensions.DependencyInjection;

namespace InSpectra.Gen.OpenCli.Composition;

/// <summary>
/// Public composition entry point for the OpenCLI logical module inside
/// <c>InSpectra.Gen</c>. Registers the schema, document, processing, and
/// acquisition-support services the generation pipeline needs.
/// </summary>
public static class OpenCliServiceCollectionExtensions
{
    /// <summary>
    /// Registers the OpenCLI schema, document, processing, acquisition, and
    /// generation services into the provided service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInSpectraOpenCli(this IServiceCollection services)
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

        return services;
    }
}
