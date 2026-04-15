namespace InSpectra.Lib.Tooling.NuGet;


using System.Text.Json;
using System.Text.Json.Serialization;

public sealed record NuGetServiceIndex(
    IReadOnlyList<NuGetServiceResource> Resources)
{
    public string GetRequiredResource(params string[] preferredTypes)
    {
        foreach (var preferredType in preferredTypes)
        {
            var resource = Resources.FirstOrDefault(candidate =>
                string.Equals(candidate.Type, preferredType, StringComparison.OrdinalIgnoreCase));

            if (resource is not null)
            {
                return resource.Id;
            }
        }

        throw new InvalidOperationException($"Could not find any of the required service resources: {string.Join(", ", preferredTypes)}.");
    }
}

public sealed record NuGetServiceResource(
    string Id,
    string Type);

public sealed record CatalogIndex(
    IReadOnlyList<CatalogPageReference> Items);

public sealed record CatalogPageReference(
    string Id,
    DateTimeOffset CommitTimeStamp);

public sealed record CatalogPage(
    IReadOnlyList<CatalogPageItem> Items);

public sealed record CatalogPageItem(
    string Id,
    string Type,
    DateTimeOffset CommitTimeStamp,
    string PackageId,
    string PackageVersion);

public sealed record SearchResponse(
    int TotalHits,
    IReadOnlyList<SearchPackage> Data);

public sealed record SearchPackage(
    string Id,
    long TotalDownloads);

public sealed record AutocompleteResponse(
    int TotalHits,
    IReadOnlyList<string> Data);

public sealed record RegistrationIndex(
    string Id,
    IReadOnlyList<RegistrationPageReference> Items);

public sealed record RegistrationPageReference(
    string Id,
    int Count,
    IReadOnlyList<RegistrationPageLeaf>? Items);

public sealed record RegistrationPage(
    IReadOnlyList<RegistrationPageLeaf> Items);

public sealed record RegistrationPageLeaf(
    string? Id,
    DateTimeOffset CommitTimeStamp,
    CatalogEntry CatalogEntry,
    string PackageContent,
    bool HasEmbeddedCatalogEntry);

public sealed record RegistrationLeafDocument(
    string? Id,
    string CatalogEntryUrl,
    bool? Listed,
    string PackageContent,
    DateTimeOffset? Published);

[JsonConverter(typeof(CatalogRepositoryJsonConverter))]
public sealed record CatalogRepository(
    string? Type,
    string? Url,
    string? Commit);

public sealed record CatalogEntry(
    string Id,
    string? Authors,
    string? Description,
    string? LicenseExpression,
    string? LicenseUrl,
    bool? Listed,
    string? ProjectUrl,
    DateTimeOffset? Published,
    CatalogRepository? Repository,
    string? ReadmeUrl,
    string Version);

public sealed record CatalogLeaf(
    string Id,
    string? Title,
    string? Description,
    string? ProjectUrl,
    CatalogRepository? Repository,
    IReadOnlyList<CatalogPackageEntry>? PackageEntries,
    IReadOnlyList<CatalogDependencyGroup>? DependencyGroups,
    JsonElement? PackageTypes);

public sealed record CatalogPackageEntry(
    string FullName,
    string Name);

public sealed record CatalogPackageType(
    string Name,
    string? Version);

public sealed record CatalogDependencyGroup(
    IReadOnlyList<CatalogDependency>? Dependencies);

public sealed record CatalogDependency(
    string Id);

