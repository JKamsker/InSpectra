using InSpectra.Gen.Acquisition.Frameworks;

namespace InSpectra.Gen.Targets;

internal sealed record LocalCliFrameworkDetection(
    string? CliFramework,
    string? HookCliFramework,
    bool HasManagedAssemblies);

public sealed class LocalCliFrameworkDetector
{
    internal LocalCliFrameworkDetection Detect(string installDirectory)
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
