using InSpectra.Gen.Acquisition.Composition;
using InSpectra.Gen.OpenCli.Composition;
using InSpectra.Gen.Rendering.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace InSpectra.Gen.Composition;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInSpectraGen(this IServiceCollection services)
    {
        services.AddInSpectraOpenCli();
        services.AddTargetServices();
        services.AddInSpectraRendering();
        services.AddInSpectraAcquisition();
        services.Configure<ViewerBundleLocatorOptions>(_ => { });

        return services;
    }

    private static IServiceCollection AddTargetServices(this IServiceCollection services)
    {
        services.AddSingleton<ExecutableResolver>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<LocalCliTargetFactory>();
        services.AddSingleton<PackageCliTargetFactory>();
        services.AddSingleton<DotnetBuildOutputResolver>();

        return services;
    }
}
