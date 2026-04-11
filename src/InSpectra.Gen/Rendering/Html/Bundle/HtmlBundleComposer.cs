using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using InSpectra.Gen.Output.Json;
using InSpectra.Gen.Rendering.Contracts;
using InSpectra.Gen.Rendering.Pipeline;

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
            ? GzipBase64(json)
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
    {
        var staticHtml = File.ReadAllText(Path.Combine(bundleRoot, "static.html"));
        var css = string.Join(
            "\n",
            EnumerateLinkedAssetPaths(staticHtml, "link", ".css")
                .Select(path => ResolveOutputAssetPath(outputDirectory, path))
                .Where(File.Exists)
                .Select(File.ReadAllText));
        var js = string.Join(
            "\n",
            EnumerateLinkedAssetPaths(staticHtml, "script", ".js")
                .Select(path => ResolveOutputAssetPath(outputDirectory, path))
                .Where(File.Exists)
                .Select(BuildInlineScript));
        var bootstrap = HtmlBundleBootstrapSupport.BuildRawBootstrapJson(
            prepared,
            options.IncludeHidden,
            options.IncludeMetadata,
            features,
            label,
            title,
            commandPrefix,
            themeOptions);
        var pack = JsonSerializer.Serialize(new { c = css, j = js, b = bootstrap }, JsonOutput.CompactSerializerOptions);
        var compressedBlob = GzipBase64(pack);

        const string head =
            """<!doctype html><html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0"><title>InSpectraUI</title><link rel="preconnect" href="https://fonts.googleapis.com"><link rel="preconnect" href="https://fonts.gstatic.com" crossorigin><link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500;700&display=swap" rel="stylesheet"></head><body><div id="root"></div>""";
        const string themeScript =
            """<script>(function(){var s=localStorage.getItem("inspectra-theme");if(s==="dark"||s==="light")document.documentElement.dataset.theme=s;else if(matchMedia("(prefers-color-scheme:dark)").matches)document.documentElement.dataset.theme="dark";var c=localStorage.getItem("inspectra-color-theme");if(c)document.documentElement.dataset.colorTheme=c})()</script>""";
        const string decompressor =
            """<script>var _u=Uint8Array.from(atob(document.getElementById("_z").textContent),function(c){return c.charCodeAt(0)});new Response(new Blob([_u]).stream().pipeThrough(new DecompressionStream("gzip"))).text().then(function(t){var p=JSON.parse(t);var d=document;var s=d.createElement("style");s.textContent=p.c;d.head.appendChild(s);var b=d.createElement("script");b.id="inspectra-bootstrap";b.type="application/json";b.textContent=p.b;d.body.appendChild(b);var j=d.createElement("script");j.textContent=p.j;d.body.appendChild(j)})</script>""";

        return head + themeScript
            + """<script id="_z" type="text/plain">""" + compressedBlob + "</script>"
            + decompressor + "</body></html>";
    }

    public static string InlineAssets(string html, string outputDirectory)
    {
        html = Regex.Replace(html, @"<link\s[^>]*href=""\./(assets/[^""]+\.css)""[^>]*/?>", match =>
        {
            var cssPath = Path.Combine(outputDirectory, match.Groups[1].Value);
            return !File.Exists(cssPath)
                ? match.Value
                : $"<style>{File.ReadAllText(cssPath)}</style>";
        });

        html = Regex.Replace(html, @"<link\s[^>]*rel=""modulepreload""[^>]*/?>[\r\n]*", string.Empty);
        html = Regex.Replace(html, @"<script\s[^>]*src=""\./(assets/[^""]+\.js)""[^>]*></script>[\r\n]*", match =>
        {
            var entryPath = ResolveOutputAssetPath(outputDirectory, match.Groups[1].Value);
            if (!File.Exists(entryPath))
            {
                return match.Value;
            }

            return $"<script>{BuildInlineScript(entryPath)}</script>";
        });

        return html;
    }

    public static string MinifyHtml(string html)
    {
        html = Regex.Replace(html, @">\s+<", "> <");
        html = Regex.Replace(html, @"^\s+", string.Empty, RegexOptions.Multiline);
        html = Regex.Replace(html, @"\n{2,}", "\n");
        return html.Trim();
    }

    public static HashSet<string> CollectReferencedAssets(string bundleRoot)
    {
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "static.html" };
        var staticHtmlPath = Path.Combine(bundleRoot, "static.html");
        if (!File.Exists(staticHtmlPath))
        {
            return referenced;
        }

        var html = File.ReadAllText(staticHtmlPath);
        foreach (Match match in Regex.Matches(html, @"(?:src|href)=""\./([^""]+)"""))
        {
            referenced.Add(match.Groups[1].Value);
        }

        return referenced;
    }

    private static string GzipBase64(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(bytes);
        }

        return Convert.ToBase64String(output.ToArray());
    }

    private static IEnumerable<string> EnumerateLinkedAssetPaths(string html, string tagName, string extension)
    {
        var attributeName = string.Equals(tagName, "script", StringComparison.OrdinalIgnoreCase) ? "src" : "href";
        var pattern = $@"<{tagName}\s[^>]*{attributeName}=""\./([^""]+{Regex.Escape(extension)})""[^>]*>";
        foreach (Match match in Regex.Matches(html, pattern))
        {
            yield return match.Groups[1].Value;
        }
    }

    private static string ResolveOutputAssetPath(string outputDirectory, string relativeAssetPath)
        => Path.Combine(outputDirectory, relativeAssetPath.Replace('/', Path.DirectorySeparatorChar));

    private static string BuildInlineScript(string entryPath)
    {
        var entryCode = File.ReadAllText(entryPath);
        var entryDirectory = Path.GetDirectoryName(entryPath)!;
        return HtmlBundleModuleSupport.BundleModulesAsIife(entryCode, entryDirectory);
    }
}
