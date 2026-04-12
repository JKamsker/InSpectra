using InSpectra.Lib.Contracts.Providers;

namespace InSpectra.Lib.Tooling.FrameworkDetection;

/// <summary>
/// Scans a local install directory for managed CLI tool assemblies and determines
/// which CLI framework(s) ship with the tool. Implements the public
/// <see cref="ILocalCliFrameworkDetector"/> contract so the app shell can depend on
/// Contracts only.
/// </summary>
internal sealed class LocalCliFrameworkDetector : ILocalCliFrameworkDetector
{
    public LocalCliFrameworkDetection Detect(string installDirectory)
    {
        if (!Directory.Exists(installDirectory))
        {
            return new LocalCliFrameworkDetection(null, null, false);
        }

        var assemblyNames = Directory.EnumerateFiles(installDirectory, "*.*", SearchOption.AllDirectories)
            .Where(static path =>
                path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (assemblyNames.Count == 0)
        {
            return new LocalCliFrameworkDetection(null, null, false);
        }

        var frameworks = CliFrameworkProviderRegistry.ResolveRuntimeReferenceProbes()
            .Where(probe => probe.RuntimeAssemblyNames.Any(assemblyNames.Contains))
            .Select(probe => probe.FrameworkName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var cliFramework = CliFrameworkProviderRegistry.CombineFrameworkNames(frameworks);
        var hookCliFramework = CliFrameworkProviderRegistry.CombineFrameworkNames(
            CliFrameworkProviderRegistry.ResolveAnalysisProviders(cliFramework)
                .Where(static provider => provider.SupportsHookAnalysis)
                .Select(static provider => provider.Name));

        return new LocalCliFrameworkDetection(cliFramework, hookCliFramework, true);
    }
}
