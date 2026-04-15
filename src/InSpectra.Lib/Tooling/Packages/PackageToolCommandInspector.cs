namespace InSpectra.Lib.Tooling.Packages;

using InSpectra.Lib.Tooling.NuGet;
using InSpectra.Lib.Tooling.Packages.Archive;

public sealed class PackageToolCommandInspector
{
    private readonly NuGetApiClient _apiClient;

    public PackageToolCommandInspector(NuGetApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<DotnetToolPackageLayout> InspectAsync(string packageContentUrl, CancellationToken cancellationToken)
        => PackageArchiveInspectionSupport.InspectAsync(
            _apiClient,
            packageContentUrl,
            "inspectra-tool-command",
            DotnetToolPackageLayoutReader.Read,
            cancellationToken);
}

