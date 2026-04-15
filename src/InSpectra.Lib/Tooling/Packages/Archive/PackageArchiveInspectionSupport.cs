namespace InSpectra.Lib.Tooling.Packages.Archive;

using InSpectra.Lib.Tooling.NuGet;

using System.IO.Compression;

public static class PackageArchiveInspectionSupport
{
    public static async Task<TResult> InspectAsync<TResult>(
        NuGetApiClient apiClient,
        string packageContentUrl,
        string tempFilePrefix,
        Func<ZipArchive, TResult> inspector,
        CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{tempFilePrefix}-{Guid.NewGuid():N}.nupkg");

        try
        {
            await apiClient.DownloadFileAsync(packageContentUrl, tempPath, cancellationToken);
            using var archive = ZipFile.OpenRead(tempPath);
            return inspector(archive);
        }
        finally
        {
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
            }
        }
    }
}

