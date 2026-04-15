using InSpectra.Lib.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace InSpectra.Gen.Hosting;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInSpectraGen(this IServiceCollection services)
    {
        services.AddInSpectraEngine();

        return services;
    }
}
