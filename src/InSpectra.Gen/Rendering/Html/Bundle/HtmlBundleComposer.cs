namespace InSpectra.Gen.Rendering.Html.Bundle;

internal static class HtmlBundleComposer
{
    public static string BuildInlineBootstrap(
        AcquiredRenderDocument prepared,
        bool includeHidden,
        bool includeMetadata,
        HtmlFeatureFlags features,
        string? label,
        string? title,
        string? commandPrefix,
        HtmlThemeOptions? themeOptions,
        int compressLevel)
    {
        var json = HtmlBundleBootstrapSupport.BuildRawBootstrapJson(
            prepared,
            includeHidden,
            includeMetadata,
            features,
            label,
            title,
            commandPrefix,
            themeOptions);
        return compressLevel >= 1
            ? HtmlBundleCompression.GzipBase64(json)
            : json.Replace("</script", "<\\/script", StringComparison.OrdinalIgnoreCase);
    }

    public static string BuildSelfExtractingHtml(
        AcquiredRenderDocument prepared,
        RenderExecutionOptions options,
        HtmlFeatureFlags features,
        string? label,
        string? title,
        string? commandPrefix,
        HtmlThemeOptions? themeOptions,
        string bundleRoot,
        string outputDirectory)
        => HtmlBundleAssetComposer.BuildSelfExtractingHtml(
            prepared,
            options,
            features,
            label,
            title,
            commandPrefix,
            themeOptions,
            bundleRoot,
            outputDirectory);

    public static string InlineAssets(string html, string outputDirectory)
        => HtmlBundleAssetComposer.InlineAssets(html, outputDirectory);

    public static string MinifyHtml(string html)
    {
        html = System.Text.RegularExpressions.Regex.Replace(html, @">\s+<", "> <");
        html = System.Text.RegularExpressions.Regex.Replace(html, @"^\s+", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\n{2,}", "\n");
        return html.Trim();
    }

    public static HashSet<string> CollectReferencedAssets(string bundleRoot)
        => HtmlBundleAssetComposer.CollectReferencedAssets(bundleRoot);
}
