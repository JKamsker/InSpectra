namespace InSpectra.Lib.Tests.Live;

using InSpectra.Lib.Tooling.Json;
using InSpectra.Lib.Tooling.NuGet;
using InSpectra.Lib.Tooling.Paths;

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

using Xunit;

internal static class NuGetApiClientLiveTestSupport
{
    private const string ScopeEnvVar = "INSPECTRA_GEN_LIVE_NUGET_SCOPE";

    public static IndexedPackageMetadata[] LoadIndexedPackageMetadata()
    {
        var repositoryRoot = RepositoryPathResolver.ResolveRepositoryRoot();
        var packageRoot = Path.Combine(repositoryRoot, "index", "packages");
        Assert.True(
            Directory.Exists(packageRoot),
            $"Missing indexed package metadata at '{packageRoot}'. Live NuGet tests require tracked fixtures under index/packages/.");

        var metadataFiles = string.Equals(Environment.GetEnvironmentVariable(ScopeEnvVar), "all", StringComparison.OrdinalIgnoreCase)
            ? Directory.EnumerateFiles(packageRoot, "metadata.json", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}latest{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            : Directory.EnumerateFiles(packageRoot, "metadata.json", SearchOption.AllDirectories)
                .Where(path => path.Contains($"{Path.DirectorySeparatorChar}latest{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        var selectedFiles = metadataFiles
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.NotEmpty(selectedFiles);

        return selectedFiles
            .Select(LoadIndexedPackageMetadata)
            .ToArray();
    }

    public static async Task<RegistrationPageLeaf?> FindLeafAsync(
        NuGetApiClient client,
        RegistrationIndex index,
        string version,
        CancellationToken cancellationToken)
    {
        foreach (var pageReference in index.Items)
        {
            var items = pageReference.Items
                ?? (await client.GetRegistrationPageAsync(pageReference.Id, cancellationToken)).Items;

            var match = items.FirstOrDefault(item =>
                string.Equals(item.CatalogEntry.Version, version, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    public static void AssertFailures(ConcurrentQueue<string> failures)
    {
        if (failures.IsEmpty)
        {
            return;
        }

        var sample = failures.Take(20).ToArray();
        var message = string.Join(Environment.NewLine, sample);
        if (failures.Count > sample.Length)
        {
            message += Environment.NewLine + $"... plus {failures.Count - sample.Length} more failures.";
        }

        Assert.Fail(message);
    }

    private static IndexedPackageMetadata LoadIndexedPackageMetadata(string metadataPath)
    {
        var payload = JsonSerializer.Deserialize<IndexedPackageMetadata>(
            File.ReadAllText(metadataPath),
            JsonOptions.Default);

        return payload ?? throw new InvalidOperationException($"Failed to deserialize indexed metadata '{metadataPath}'.");
    }
}

internal sealed record IndexedPackageMetadata(
    [property: JsonPropertyName("packageId")] string PackageId,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("registrationLeafUrl")] string RegistrationLeafUrl,
    [property: JsonPropertyName("catalogEntryUrl")] string CatalogEntryUrl);
