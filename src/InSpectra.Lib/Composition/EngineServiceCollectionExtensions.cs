using InSpectra.Lib.Contracts.Providers;
using InSpectra.Lib.Execution.Process;
using InSpectra.Lib.Modes.CliFx.Execution;
using InSpectra.Lib.Modes.CliFx.Metadata;
using InSpectra.Lib.Modes.CliFx.Projection;
using InSpectra.Lib.Modes.Help.Crawling;
using InSpectra.Lib.Modes.Help.Projection;
using InSpectra.Lib.Modes.Hook.Execution;
using InSpectra.Lib.Modes.Native.Execution;
using InSpectra.Lib.Modes.Static.Inspection;
using InSpectra.Lib.Modes.Static.Projection;
using InSpectra.Lib.OpenCli.Composition;
using InSpectra.Lib.Orchestration;
using InSpectra.Lib.Rendering.Composition;
using InSpectra.Lib.Targets.Sources;
using InSpectra.Lib.Tooling.FrameworkDetection;
using InSpectra.Lib.Tooling.Process;
using InSpectra.Lib.Tooling.Tools;
using InSpectra.Lib.UseCases.Generate.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace InSpectra.Lib.Composition;

/// <summary>
/// Public composition entry point for the <c>InSpectra.Lib</c> module.
/// Registers the concrete acquisition analyzers, runtimes, and support services the
/// app shell needs without requiring reach-in via <c>InternalsVisibleTo</c>.
/// </summary>
public static class EngineServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full acquisition pipeline (CliFx, static analysis, hook, and
    /// shared command/tool infrastructure) into the provided service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInSpectraEngine(this IServiceCollection services)
    {
        services.AddInSpectraOpenCli();
        services.AddInSpectraGenerateUseCases();
        services.AddExecutionServices();
        services.AddTargetServices();
        services.AddInSpectraRendering();

        services.AddSingleton<CommandRuntime>();
        services.AddSingleton<Crawler>();
        services.AddSingleton<IHelpCrawler>(sp => sp.GetRequiredService<Crawler>());
        services.AddSingleton<OpenCliBuilder>();
        services.AddSingleton<IToolDescriptorResolver, ToolDescriptorResolver>();
        services.AddSingleton<CliFxMetadataInspector>();
        services.AddSingleton<CliFxOpenCliBuilder>();
        services.AddSingleton<CliFxCoverageClassifier>();
        services.AddSingleton<StaticAnalysisRuntime>();
        services.AddSingleton<DnlibAssemblyScanner>();
        services.AddSingleton<StaticAnalysisAssemblyInspectionSupport>();
        services.AddSingleton<StaticAnalysisOpenCliBuilder>();
        services.AddSingleton<StaticAnalysisCoverageClassifier>();
        services.AddSingleton<InstalledToolAnalyzer>();
        services.AddSingleton<CliFxInstalledToolAnalysisSupport>();
        services.AddSingleton<StaticInstalledToolAnalysisSupport>();
        services.AddSingleton<HookInstalledToolAnalysisSupport>();
        services.AddSingleton<NativeInstalledToolAnalysisSupport>();
        services.AddSingleton<AcquisitionAnalysisDispatcher>();

        // Public composition seams for the app shell. These adapters let
        // `InSpectra.Gen` depend on `Contracts.Providers` only, with no reach-in
        // via `InternalsVisibleTo`.
        services.AddSingleton<ICliFrameworkCatalog, CliFrameworkCatalogAdapter>();
        services.AddSingleton<ILocalCliFrameworkDetector, LocalCliFrameworkDetector>();
        services.AddSingleton<IPackageCliToolInstaller, PackageCliToolInstaller>();
        services.AddSingleton<IAcquisitionAnalysisDispatcher>(sp => sp.GetRequiredService<AcquisitionAnalysisDispatcher>());
        services.AddSingleton<IAcquisitionAnalysisDispatcherInternal>(sp => sp.GetRequiredService<AcquisitionAnalysisDispatcher>());

        return services;
    }

    private static IServiceCollection AddExecutionServices(this IServiceCollection services)
    {
        services.AddSingleton<ExecutableResolver>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();

        return services;
    }

    private static IServiceCollection AddTargetServices(this IServiceCollection services)
    {
        services.AddSingleton<LocalCliTargetFactory>();
        services.AddSingleton<PackageCliTargetFactory>();
        services.AddSingleton<DotnetBuildOutputResolver>();

        return services;
    }
}
