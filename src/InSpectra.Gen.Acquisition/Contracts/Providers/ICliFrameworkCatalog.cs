namespace InSpectra.Gen.Acquisition.Contracts.Providers;

/// <summary>
/// Contracts-level description of a single known CLI framework provider. Only the fields
/// the app shell needs to plan acquisition attempts and detect locally installed tools
/// are exposed here; the full provider record stays inside
/// <c>InSpectra.Gen.Acquisition.Tooling.FrameworkDetection</c>.
/// </summary>
public sealed record CliFrameworkCatalogEntry(
    string Name,
    bool SupportsCliFxAnalysis,
    bool SupportsHookAnalysis,
    bool SupportsStaticAnalysis,
    IReadOnlyList<string> RuntimeAssemblyNames);

/// <summary>
/// Public composition seam for the CLI framework detection catalog. Lets the app shell
/// plan acquisition attempts and detect locally installed tools without touching
/// <c>InSpectra.Gen.Acquisition.Tooling.FrameworkDetection</c> directly.
/// </summary>
public interface ICliFrameworkCatalog
{
    /// <summary>
    /// Returns the set of providers the caller should probe against when
    /// <paramref name="cliFramework"/> is specified (or empty when it is null/blank).
    /// </summary>
    IReadOnlyList<CliFrameworkCatalogEntry> ResolveAnalysisProviders(string? cliFramework);

    /// <summary>
    /// Returns the full set of runtime reference probes for every registered framework.
    /// Used during local disk scans to infer which framework a tool ships with.
    /// </summary>
    IReadOnlyList<CliFrameworkCatalogEntry> GetAllFrameworks();

    /// <summary>
    /// Returns the split framework names for <paramref name="cliFramework"/>, using the
    /// provider catalog to handle aliases and combined labels.
    /// </summary>
    IReadOnlyList<string> ResolveFrameworkNames(string? cliFramework);

    /// <summary>
    /// Combines the supplied framework names into a single display string using the
    /// registry's canonical ordering. Returns <see langword="null"/> when empty.
    /// </summary>
    string? CombineFrameworkNames(IEnumerable<string> frameworkNames);
}
