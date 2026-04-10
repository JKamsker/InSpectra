namespace InSpectra.Gen.Acquisition.Analysis.NonSpectre;

using InSpectra.Gen.Acquisition.Packages;

using InSpectra.Gen.Acquisition.Infrastructure.Paths;

using InSpectra.Gen.Acquisition.NuGet;

using InSpectra.Discovery.Tool.Analysis;

using System.Text.Json.Nodes;

internal static class NonSpectreBootstrapSupport
{
    public static async Task<NonSpectreAnalysisBootstrapResult> PopulateResultAsync(
        JsonObject result,
        NuGetApiClient apiClient,
        string packageId,
        string version,
        string? commandName,
        CancellationToken cancellationToken)
    {
        var (registrationLeaf, catalogLeaf) = await PackageVersionResolver.ResolveAsync(apiClient, packageId, version, cancellationToken);
        ApplyPackageMetadata(result, packageId, version, registrationLeaf, catalogLeaf);

        return new NonSpectreAnalysisBootstrapResult(
            registrationLeaf.PackageContent,
            await ResolveCommandInfoAsync(apiClient, registrationLeaf.PackageContent, commandName, cancellationToken));
    }

    private static void ApplyPackageMetadata(
        JsonObject result,
        string packageId,
        string version,
        RegistrationLeafDocument registrationLeaf,
        CatalogLeaf catalogLeaf)
    {
        result["packageUrl"] = $"https://www.nuget.org/packages/{packageId}/{version}";
        result["projectUrl"] = catalogLeaf.ProjectUrl;
        result["sourceRepositoryUrl"] = PackageVersionResolver.NormalizeRepositoryUrl(catalogLeaf.Repository?.Url);
        result["registrationLeafUrl"] = registrationLeaf.Id;
        result["catalogEntryUrl"] = registrationLeaf.CatalogEntryUrl;
        result["packageContentUrl"] = registrationLeaf.PackageContent;
        result["publishedAt"] = registrationLeaf.Published?.ToUniversalTime().ToString("O");
        result[ResultKey.NugetTitle] = catalogLeaf.Title;
        result[ResultKey.NugetDescription] = catalogLeaf.Description;
    }

    private static async Task<ResolvedToolCommandInfo> ResolveCommandInfoAsync(
        NuGetApiClient apiClient,
        string packageContentUrl,
        string? commandName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(commandName))
        {
            return new ResolvedToolCommandInfo(commandName, null, null);
        }

        var packageLayout = await new PackageToolCommandInspector(apiClient).InspectAsync(packageContentUrl, cancellationToken);
        return new ResolvedToolCommandInfo(
            packageLayout.ToolCommandNames.FirstOrDefault(),
            packageLayout.ToolEntryPointPaths.FirstOrDefault(),
            packageLayout.ToolSettingsPaths.FirstOrDefault());
    }
}


