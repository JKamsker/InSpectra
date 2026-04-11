using InSpectra.Gen.Acquisition.Analysis.CliFx.Execution;
using InSpectra.Gen.Acquisition.Analysis.CliFx.Metadata;
using InSpectra.Gen.Acquisition.Analysis.CliFx.OpenCli;
using InSpectra.Gen.Acquisition.Analysis.Hook;
using InSpectra.Gen.Acquisition.Analysis.Tools;
using InSpectra.Gen.Acquisition.Help.Crawling;
using InSpectra.Gen.Acquisition.Help.OpenCli;
using InSpectra.Gen.Acquisition.Infrastructure.Commands;
using InSpectra.Gen.Acquisition.StaticAnalysis.Inspection;
using InSpectra.Gen.Acquisition.StaticAnalysis.OpenCli;
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

        return services;
    }
}
