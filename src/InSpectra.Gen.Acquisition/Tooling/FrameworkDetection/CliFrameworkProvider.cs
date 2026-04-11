namespace InSpectra.Gen.Acquisition.Tooling.FrameworkDetection;


internal sealed record CliFrameworkProvider(
    string Name,
    IReadOnlyList<string> LabelAliases,
    IReadOnlyList<string> DependencyIds,
    IReadOnlyList<string> PackageAssemblyNames,
    IReadOnlyList<string> RuntimeAssemblyNames,
    bool SupportsCliFxAnalysis,
    bool SupportsHookAnalysis,
    StaticAnalysisFrameworkAdapter? StaticAnalysisAdapter)
{
    public bool Matches(IReadOnlySet<string> dependencyIds, IReadOnlySet<string> assemblyNames)
        => DependencyIds.Any(dependencyIds.Contains) || PackageAssemblyNames.Any(assemblyNames.Contains);

    public IEnumerable<string> EnumerateLabels()
    {
        yield return Name;

        foreach (var alias in LabelAliases)
        {
            if (!string.IsNullOrWhiteSpace(alias))
            {
                yield return alias;
            }
        }
    }
}

/// <summary>
/// Type-erased carrier for a Static-mode attribute reader. The Registry keeps the reader
/// as <see cref="object"/> so that <c>Tooling/</c> has no compile-time dependency on
/// <c>Modes.Static.Attributes</c>. Consumers in Static mode cast <see cref="Reader"/> back
/// to <c>IStaticAttributeReader</c> at use sites.
/// </summary>
internal sealed record StaticAnalysisFrameworkAdapter(
    string FrameworkName,
    string AssemblyName,
    object Reader);
