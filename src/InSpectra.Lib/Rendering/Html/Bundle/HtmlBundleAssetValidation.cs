using InSpectra.Lib;

namespace InSpectra.Lib.Rendering.Html.Bundle;

internal static class HtmlBundleAssetValidation
{
    public static void AssertReferencedAssetsExist(string bundleRoot, IEnumerable<string> referencedAssets)
    {
        var missingAssets = referencedAssets
            .Where(relativePath => !File.Exists(ResolveBundleAssetPath(bundleRoot, relativePath)))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        if (missingAssets.Length == 0)
        {
            return;
        }

        throw new CliUsageException(
            $"InSpectra.UI bundle at `{bundleRoot}` is incomplete.",
            [.. missingAssets.Select(asset => $"Missing asset: `{asset}`")]);
    }

    private static string ResolveBundleAssetPath(string bundleRoot, string relativeAssetPath)
        => Path.Combine(bundleRoot, relativeAssetPath.Replace('/', Path.DirectorySeparatorChar));
}
