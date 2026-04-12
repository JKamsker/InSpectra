using InSpectra.Gen.Engine.Contracts.Providers;
using InSpectra.Gen.Engine.Execution.Process;
using InSpectra.Gen.Engine.Modes.CliFx.Execution;
using InSpectra.Gen.Engine.Modes.CliFx.Metadata;
using InSpectra.Gen.Engine.Modes.CliFx.Projection;
using InSpectra.Gen.Engine.Modes.Help.Crawling;
using InSpectra.Gen.Engine.Modes.Help.Projection;
using InSpectra.Gen.Engine.Modes.Hook.Execution;
using InSpectra.Gen.Engine.Modes.Static.Inspection;
using InSpectra.Gen.Engine.Modes.Static.Projection;
using InSpectra.Gen.Engine.OpenCli.Composition;
using InSpectra.Gen.Engine.Orchestration;
using InSpectra.Gen.Engine.Rendering.Composition;
using InSpectra.Gen.Engine.Targets.Sources;
using InSpectra.Gen.Engine.Tooling.FrameworkDetection;
using InSpectra.Gen.Engine.Tooling.Process;
using InSpectra.Gen.Engine.Tooling.Tools;
using InSpectra.Gen.Engine.UseCases.Generate.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace InSpectra.Gen.Engine.Composition;

/// <summary>
/// Public composition entry point for the <c>InSpectra.Gen.Engine</c> module.
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

        // Public composition seams for the app shell. These adapters let
        // `InSpectra.Gen` depend on `Contracts.Providers` only, with no reach-in
        // via `InternalsVisibleTo`.
        services.AddSingleton<ICliFrameworkCatalog, CliFrameworkCatalogAdapter>();
        services.AddSingleton<ILocalCliFrameworkDetector, LocalCliFrameworkDetector>();
        services.AddSingleton<IPackageCliToolInstaller, PackageCliToolInstaller>();
        services.AddSingleton<IAcquisitionAnalysisDispatcher, AcquisitionAnalysisDispatcher>();

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
