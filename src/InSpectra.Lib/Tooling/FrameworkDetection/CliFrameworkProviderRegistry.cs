namespace InSpectra.Lib.Tooling.FrameworkDetection;

using InSpectra.Lib.Contracts.Providers;
using InSpectra.Lib.Tooling.NuGet;

/// <summary>
/// Catalog of known CLI frameworks and their static-analysis adapters.
///
/// <para>
/// The registry is intentionally split into two halves to keep the <c>Tooling/</c>
/// layer free of any <c>Modes.*</c> dependency:
/// <list type="bullet">
///   <item>
///     <description>
///       Base providers (Spectre, CliFx, catalog-only frameworks) are hardcoded here
///       because they need no Static-mode attribute reader.
///     </description>
///   </item>
///   <item>
///     <description>
///       Static-analysis providers are added at startup via
///       <see cref="RegisterStaticAnalysisProvider"/>. The Static mode supplies the
///       concrete reader instances through a module initializer, so the Registry never
///       references <c>Modes.Static.Attributes</c> directly.
///     </description>
///   </item>
/// </list>
/// </para>
/// </summary>
public static class CliFrameworkProviderRegistry
{
    private static readonly List<CliFrameworkProvider> MutableProviders = CreateBaseProviders();
    private static readonly Dictionary<string, CliFrameworkProvider> ProvidersByLabel =
        new(StringComparer.OrdinalIgnoreCase);

    static CliFrameworkProviderRegistry()
    {
        RebuildLabelIndex();
    }

    /// <summary>
    /// Registers a static-analysis capable CLI framework. Called from Modes/Static via
    /// a module initializer so that the concrete reader instances live next to the
    /// mode that owns them.
    /// </summary>
    internal static void RegisterStaticAnalysisProvider(
        string name,
        IReadOnlyList<string> dependencyIds,
        IReadOnlyList<string> packageAssemblyNames,
        string staticAssemblyName,
        object reader,
        params string[] labelAliases)
    {
        var provider = new CliFrameworkProvider(
            Name: name,
            LabelAliases: labelAliases,
            DependencyIds: dependencyIds,
            PackageAssemblyNames: packageAssemblyNames,
            RuntimeAssemblyNames: [staticAssemblyName],
            SupportsCliFxAnalysis: false,
            SupportsHookAnalysis:
                string.Equals(name, "System.CommandLine", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "McMaster.Extensions.CommandLineUtils", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Microsoft.Extensions.CommandLineUtils", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "CommandLineParser", StringComparison.OrdinalIgnoreCase),
            StaticAnalysisAdapter: new StaticAnalysisFrameworkAdapter(name, staticAssemblyName, reader));

        MutableProviders.Add(provider);
        RebuildLabelIndex();
    }

    public static string? Detect(CatalogLeaf catalogLeaf)
    {
        var dependencyIds = (catalogLeaf.DependencyGroups ?? [])
            .SelectMany(group => group.Dependencies ?? [])
            .Select(dependency => dependency.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var assemblyNames = (catalogLeaf.PackageEntries ?? [])
            .Select(entry => entry.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matches = MutableProviders
            .Where(provider => provider.Matches(dependencyIds, assemblyNames))
            .Select(provider => provider.Name)
            .ToArray();

        return matches.Length == 0
            ? null
            : string.Join(" + ", matches);
    }

    internal static IReadOnlyList<CliFrameworkReferenceProbe> ResolveRuntimeReferenceProbes()
        => MutableProviders
            .Select(static provider => new CliFrameworkReferenceProbe(
                provider.Name,
                provider.PackageAssemblyNames,
                provider.RuntimeAssemblyNames))
            .ToArray();

    public static bool HasCliFxAnalysisSupport(string? cliFramework)
        => ResolveAnalysisProviders(cliFramework).Any(static provider => provider.SupportsCliFxAnalysis);

    public static bool HasStaticAnalysisSupport(string? cliFramework)
        => ResolveStaticAnalysisAdapter(cliFramework) is not null;

    public static bool HasHookAnalysisSupport(string? cliFramework)
        => ResolveAnalysisProviders(cliFramework).Any(static provider => provider.SupportsHookAnalysis);

    public static string? ResolveHookAnalysisFramework(string? cliFramework)
        => ResolveAnalysisProviders(cliFramework)
            .Where(static provider => provider.SupportsHookAnalysis)
            .Select(static provider => provider.Name)
            .FirstOrDefault();

    internal static StaticAnalysisFrameworkAdapter? ResolveStaticAnalysisAdapter(string? cliFramework)
    {
        foreach (var provider in ResolveAnalysisProviderDetails(cliFramework))
        {
            if (provider.StaticAnalysisAdapter is not null)
            {
                return provider.StaticAnalysisAdapter;
            }
        }

        return null;
    }

    public static bool ShouldReplace(string? existingCliFramework, string? candidateCliFramework)
    {
        if (string.IsNullOrWhiteSpace(candidateCliFramework))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(existingCliFramework))
        {
            return true;
        }

        if (string.Equals(existingCliFramework, candidateCliFramework, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!HasCliFxAnalysisSupport(candidateCliFramework))
        {
            return false;
        }

        return !HasCliFxAnalysisSupport(existingCliFramework)
            || string.Equals(existingCliFramework, "CliFx", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<CliFrameworkCatalogEntry> ResolveAnalysisProviders(string? cliFramework)
        => ResolveAnalysisProviderDetails(cliFramework)
            .Select(static provider => new CliFrameworkCatalogEntry(
                Name: provider.Name,
                SupportsCliFxAnalysis: provider.SupportsCliFxAnalysis,
                SupportsHookAnalysis: provider.SupportsHookAnalysis,
                SupportsStaticAnalysis: provider.SupportsStaticAnalysis,
                RuntimeAssemblyNames: provider.RuntimeAssemblyNames))
            .ToArray();

    internal static IReadOnlyList<CliFrameworkProvider> ResolveAnalysisProviderDetails(string? cliFramework)
    {
        if (string.IsNullOrWhiteSpace(cliFramework))
        {
            return [];
        }

        var matchedProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in cliFramework.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (ProvidersByLabel.TryGetValue(part, out var provider))
            {
                matchedProviders.Add(provider.Name);
            }
        }

        var providers = new List<CliFrameworkProvider>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in MutableProviders)
        {
            if (matchedProviders.Contains(provider.Name)
                && seen.Add(provider.Name))
            {
                providers.Add(provider);
            }
        }

        return providers;
    }

    public static IReadOnlyList<string> ResolveFrameworkNames(string? cliFramework)
        => ResolveAnalysisProviders(cliFramework)
            .Select(static provider => provider.Name)
            .ToArray();

    public static string? CombineFrameworkNames(IEnumerable<string> frameworkNames)
    {
        var names = frameworkNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (names.Count == 0)
        {
            return null;
        }

        var ordered = MutableProviders
            .Where(provider => names.Contains(provider.Name))
            .Select(static provider => provider.Name)
            .ToArray();
        return ordered.Length == 0
            ? null
            : string.Join(" + ", ordered);
    }

    private static List<CliFrameworkProvider> CreateBaseProviders()
    {
        return
        [
            CreateCatalogOnlyProvider("Spectre.Console.Cli", ["Spectre.Console.Cli"], ["Spectre.Console.Cli.dll"]),
            CreateCliFxProvider(),
            CreateCatalogOnlyProvider("DocoptNet", ["DocoptNet"], ["DocoptNet.dll"]),
            CreateCatalogOnlyProvider("ConsoleAppFramework", ["ConsoleAppFramework"], ["ConsoleAppFramework.dll"]),
            CreateCatalogOnlyProvider("Oakton", ["Oakton"], ["Oakton.dll"]),
            CreateCatalogOnlyProvider("ManyConsole", ["ManyConsole"], ["ManyConsole.dll"]),
            CreateCatalogOnlyProvider("Mono.Options / NDesk.Options", ["Mono.Options", "NDesk.Options"], ["Mono.Options.dll", "NDesk.Options.dll"], "Mono.Options", "NDesk.Options"),
            CreateHookOnlyProvider("FluentCommandLineParser", ["FluentCommandLineParser"], ["FluentCommandLineParser.dll"]),
        ];
    }

    private static void RebuildLabelIndex()
    {
        ProvidersByLabel.Clear();
        foreach (var provider in MutableProviders)
        {
            foreach (var label in provider.EnumerateLabels())
            {
                ProvidersByLabel[label] = provider;
            }
        }
    }

    private static CliFrameworkProvider CreateCliFxProvider()
        => new(
            Name: "CliFx",
            LabelAliases: [],
            DependencyIds: ["CliFx"],
            PackageAssemblyNames: ["CliFx.dll"],
            RuntimeAssemblyNames: ["CliFx"],
            SupportsCliFxAnalysis: true,
            SupportsHookAnalysis: false,
            StaticAnalysisAdapter: null);

    private static CliFrameworkProvider CreateCatalogOnlyProvider(
        string name,
        IReadOnlyList<string> dependencyIds,
        IReadOnlyList<string> packageAssemblyNames,
        params string[] labelAliases)
        => new(
            Name: name,
            LabelAliases: labelAliases,
            DependencyIds: dependencyIds,
            PackageAssemblyNames: packageAssemblyNames,
            RuntimeAssemblyNames: packageAssemblyNames
                .Select(static assemblyName => Path.GetFileNameWithoutExtension(assemblyName))
                .Where(static assemblyName => !string.IsNullOrWhiteSpace(assemblyName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            SupportsCliFxAnalysis: false,
            SupportsHookAnalysis: false,
            StaticAnalysisAdapter: null);

    private static CliFrameworkProvider CreateHookOnlyProvider(
        string name,
        IReadOnlyList<string> dependencyIds,
        IReadOnlyList<string> packageAssemblyNames,
        params string[] labelAliases)
        => new(
            Name: name,
            LabelAliases: labelAliases,
            DependencyIds: dependencyIds,
            PackageAssemblyNames: packageAssemblyNames,
            RuntimeAssemblyNames: packageAssemblyNames
                .Select(static assemblyName => Path.GetFileNameWithoutExtension(assemblyName))
                .Where(static assemblyName => !string.IsNullOrWhiteSpace(assemblyName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            SupportsCliFxAnalysis: false,
            SupportsHookAnalysis: true,
            StaticAnalysisAdapter: null);
}
