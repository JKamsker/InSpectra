using Microsoft.Extensions.DependencyInjection;
using InSpectra.Lib.UseCases.Generate;

namespace InSpectra.Lib.UseCases.Generate.Composition;

/// <summary>
/// Public composition entry point for the Generate use-case services inside
/// <c>InSpectra.Gen</c>. Registers the acquisition and generation orchestrators
/// that sit on top of the OpenCLI domain services.
/// </summary>
internal static class GenerateUseCasesServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Generate use-case services (native acquisition support,
    /// acquisition orchestration, and generation orchestration) into the
    /// provided service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection for chaining.</returns>
    internal static IServiceCollection AddInSpectraGenerateUseCases(this IServiceCollection services)
    {
        services.AddSingleton<OpenCliNativeAcquisitionSupport>();
        services.AddSingleton<IOpenCliAcquisitionService, OpenCliAcquisitionService>();
        services.AddSingleton<IOpenCliGenerationService, OpenCliGenerationService>();

        return services;
    }
}
