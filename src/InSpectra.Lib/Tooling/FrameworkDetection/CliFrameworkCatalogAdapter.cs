using InSpectra.Lib.Contracts.Providers;

namespace InSpectra.Lib.Tooling.FrameworkDetection;

/// <summary>
/// Adapts <see cref="CliFrameworkProviderRegistry"/> to the public
/// <see cref="ICliFrameworkCatalog"/> contract so the app shell can plan acquisition
/// attempts and detect installed tools without reaching into
/// <c>InSpectra.Lib.Tooling.FrameworkDetection</c>.
/// </summary>
internal sealed class CliFrameworkCatalogAdapter : ICliFrameworkCatalog
{
    public IReadOnlyList<CliFrameworkCatalogEntry> ResolveAnalysisProviders(string? cliFramework)
        => CliFrameworkProviderRegistry.ResolveAnalysisProviders(cliFramework);

    public IReadOnlyList<CliFrameworkCatalogEntry> GetAllFrameworks()
        => CliFrameworkProviderRegistry.ResolveRuntimeReferenceProbes()
            .Select(probe => new CliFrameworkCatalogEntry(
                Name: probe.FrameworkName,
                SupportsCliFxAnalysis: false,
                SupportsHookAnalysis: false,
                SupportsStaticAnalysis: false,
                RuntimeAssemblyNames: probe.RuntimeAssemblyNames))
            .ToArray();

    public IReadOnlyList<string> ResolveFrameworkNames(string? cliFramework)
        => CliFrameworkProviderRegistry.ResolveFrameworkNames(cliFramework);

    public string? CombineFrameworkNames(IEnumerable<string> frameworkNames)
        => CliFrameworkProviderRegistry.CombineFrameworkNames(frameworkNames);
}
