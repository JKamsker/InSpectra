using InSpectra.Gen.Acquisition.Contracts.Providers;
using InSpectra.Gen.Acquisition.Modes.CliFx.Execution;
using InSpectra.Gen.Acquisition.Modes.CliFx.Metadata;
using InSpectra.Gen.Acquisition.Modes.CliFx.Projection;
using InSpectra.Gen.Acquisition.Modes.Hook;
using InSpectra.Gen.Acquisition.Orchestration;
using InSpectra.Gen.Acquisition.Tooling.FrameworkDetection;
using InSpectra.Gen.Acquisition.Tooling.Tools;
using InSpectra.Gen.Acquisition.Modes.Help.Crawling;
using InSpectra.Gen.Acquisition.Modes.Help.Projection;
using InSpectra.Gen.Acquisition.Tooling.Process;
using InSpectra.Gen.Acquisition.Modes.Static.Inspection;
using InSpectra.Gen.Acquisition.Modes.Static.Projection;
using Microsoft.Extensions.DependencyInjection;

namespace InSpectra.Gen.Acquisition.Composition;

/// <summary>
/// Public composition entry point for the <c>InSpectra.Gen.Acquisition</c> module.
/// Registers the concrete acquisition analyzers, runtimes, and support services the
/// app shell needs without requiring reach-in via <c>InternalsVisibleTo</c>.
/// </summary>
public static class AcquisitionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full acquisition pipeline (CliFx, static analysis, hook, and
    /// shared command/tool infrastructure) into the provided service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInSpectraAcquisition(this IServiceCollection services)
    {
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
}
