namespace InSpectra.Lib.Tooling.Tools;

using InSpectra.Lib.Tooling.FrameworkDetection;

using InSpectra.Lib.Tooling.Packages;
using InSpectra.Lib.Tooling.Packages.Archive;

using InSpectra.Lib.Contracts;
using InSpectra.Lib.Tooling.Paths;

using InSpectra.Lib.Tooling.NuGet;
using InSpectra.Lib;

public sealed class ToolDescriptorResolver : IToolDescriptorResolver
{
    public async Task<ToolDescriptorResolution> ResolveAsync(
        string packageId,
        string version,
        string? commandName,
        CancellationToken cancellationToken)
    {
        using var scope = ApplicationLifetime.CreateNuGetApiClientScope();
        var (leaf, catalogLeaf) = await PackageVersionResolver.ResolveAsync(scope.Client, packageId, version, cancellationToken);
        var packageInspection = await new PackageArchiveInspector(scope.Client).InspectAsync(leaf.PackageContent, cancellationToken);
        var resolvedCommandName = ResolveCommandName(packageId, commandName, packageInspection);
        var descriptor = ResolveFromCatalogLeaf(
            packageId,
            version,
            catalogLeaf,
            packageUrl: $"https://www.nuget.org/packages/{packageId}/{version}",
            packageContentUrl: leaf.PackageContent,
            catalogEntryUrl: leaf.CatalogEntryUrl,
            packageInspection,
            resolvedCommandName);
        return new ToolDescriptorResolution(descriptor, packageInspection);
    }

    public static ToolDescriptor ResolveFromCatalogLeaf(
        string packageId,
        string version,
        CatalogLeaf catalogLeaf,
        string? packageUrl,
        string? packageContentUrl,
        string? catalogEntryUrl,
        SpectrePackageInspection? packageInspection = null,
        string? commandName = null)
    {
        var cliFramework = DetectCliFramework(catalogLeaf, packageInspection);
        var hookCliFramework = ResolveHookCliFramework(cliFramework, packageInspection);
        var (preferredMode, reason) = SelectMode(catalogLeaf, packageInspection, cliFramework);

        return new ToolDescriptor(
            packageId,
            version,
            CommandName: commandName,
            cliFramework,
            preferredMode,
            reason,
            packageUrl ?? $"https://www.nuget.org/packages/{packageId}/{version}",
            packageContentUrl,
            catalogEntryUrl,
            PackageTitle: catalogLeaf.Title,
            PackageDescription: catalogLeaf.Description,
            HookCliFramework: hookCliFramework);
    }

    private static string? DetectCliFramework(CatalogLeaf catalogLeaf, SpectrePackageInspection? packageInspection)
    {
        if (HasConfirmedSpectreCli(catalogLeaf, packageInspection))
        {
            var classified = CliFrameworkProviderRegistry.Detect(catalogLeaf);
            return string.IsNullOrWhiteSpace(classified) || string.Equals(classified, "Spectre.Console.Cli", StringComparison.Ordinal)
                ? "Spectre.Console.Cli"
                : $"Spectre.Console.Cli + {classified}";
        }

        return CliFrameworkProviderRegistry.Detect(catalogLeaf);
    }

    private static (string PreferredMode, string Reason) SelectMode(CatalogLeaf catalogLeaf, SpectrePackageInspection? packageInspection, string? cliFramework)
        => HasConfirmedSpectreCli(catalogLeaf, packageInspection)
            ? (AnalysisMode.Native, "confirmed-spectre-console-cli")
            : CliFrameworkProviderRegistry.HasCliFxAnalysisSupport(cliFramework)
                ? (AnalysisMode.CliFx, "confirmed-clifx")
                : CliFrameworkProviderRegistry.HasStaticAnalysisSupport(cliFramework)
                    ? (
                        AnalysisMode.Static,
                        HasConfirmedStaticFramework(cliFramework, packageInspection)
                            ? "confirmed-static-analysis-framework"
                            : "candidate-static-analysis-framework")
                    : (AnalysisMode.Help, "generic-help-crawl");

    private static bool HasConfirmedStaticFramework(string? cliFramework, SpectrePackageInspection? packageInspection)
        => packageInspection is not null
            && CliFrameworkProviderRegistry.ResolveAnalysisProviderDetails(cliFramework)
                .Any(provider =>
                    provider.StaticAnalysisAdapter is not null
                    && packageInspection.HasToolAssemblyReferencingCliFramework(provider.Name));

    private static string? ResolveHookCliFramework(string? cliFramework, SpectrePackageInspection? packageInspection)
    {
        if (packageInspection is null)
        {
            return null;
        }

        return CliFrameworkProviderRegistry.CombineFrameworkNames(
            CliFrameworkProviderRegistry.ResolveAnalysisProviderDetails(cliFramework)
                .Where(provider =>
                    provider.SupportsHookAnalysis
                    && packageInspection.HasToolAssemblyReferencingCliFramework(provider.Name))
                .Select(static provider => provider.Name));
    }

    private static bool HasConfirmedSpectreCli(CatalogLeaf catalogLeaf, SpectrePackageInspection? packageInspection)
    {
        var dependencyIds = (catalogLeaf.DependencyGroups ?? [])
            .SelectMany(group => group.Dependencies ?? [])
            .Select(dependency => dependency.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray();
        var packageEntryNames = (catalogLeaf.PackageEntries ?? [])
            .Select(entry => entry.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        return dependencyIds.Any(id => string.Equals(id, "Spectre.Console.Cli", StringComparison.OrdinalIgnoreCase))
            || packageInspection?.ToolAssembliesReferencingSpectreConsoleCli.Count > 0
            || packageInspection?.SpectreConsoleCliDependencyVersions.Count > 0
            || packageEntryNames.Any(name => string.Equals(name, "Spectre.Console.Cli.dll", StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveCommandName(
        string packageId,
        string? commandName,
        SpectrePackageInspection inspection)
    {
        var commands = inspection.ToolCommandNames
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (commands.Length == 0)
        {
            throw new CliUsageException(
                $"Package `{packageId}` does not expose a .NET tool command in DotnetToolSettings.xml.");
        }

        if (!string.IsNullOrWhiteSpace(commandName))
        {
            var resolved = commands.FirstOrDefault(candidate =>
                string.Equals(candidate, commandName, StringComparison.OrdinalIgnoreCase));
            if (resolved is null)
            {
                throw new CliUsageException(
                    $"Package `{packageId}` does not expose command `{commandName}`. Available commands: {string.Join(", ", commands)}.");
            }

            return resolved;
        }

        if (commands.Length > 1)
        {
            throw new CliUsageException(
                $"Package `{packageId}` exposes multiple tool commands ({string.Join(", ", commands)}). Use `--command` to select one.");
        }

        return commands[0];
    }
}
