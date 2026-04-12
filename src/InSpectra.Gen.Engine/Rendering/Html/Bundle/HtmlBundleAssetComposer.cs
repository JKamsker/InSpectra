using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Rendering.Contracts;
using InSpectra.Gen.Engine.Rendering.Pipeline;

namespace InSpectra.Gen.Engine.Rendering.Html.Bundle;

internal static class HtmlBundleAssetComposer
{
    private static readonly JsonSerializerOptions CompactSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    private const string SelfExtractingHead =
        """<!doctype html><html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0"><title>InSpectraUI</title><link rel="preconnect" href="https://fonts.googleapis.com"><link rel="preconnect" href="https://fonts.gstatic.com" crossorigin><link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500;700&display=swap" rel="stylesheet"></head><body><div id="root"></div>""";
    private const string SelfExtractingThemeScript =
        """<script>(function(){var s=localStorage.getItem("inspectra-theme");if(s==="dark"||s==="light")document.documentElement.dataset.theme=s;else if(matchMedia("(prefers-color-scheme:dark)").matches)document.documentElement.dataset.theme="dark";var c=localStorage.getItem("inspectra-color-theme");if(c)document.documentElement.dataset.colorTheme=c})()</script>""";
    private const string SelfExtractingDecompressor =
        """<script>var _u=Uint8Array.from(atob(document.getElementById("_z").textContent),function(c){return c.charCodeAt(0)});new Response(new Blob([_u]).stream().pipeThrough(new DecompressionStream("gzip"))).text().then(function(t){var p=JSON.parse(t);var d=document;var s=d.createElement("style");s.textContent=p.c;d.head.appendChild(s);var b=d.createElement("script");b.id="inspectra-bootstrap";b.type="application/json";b.textContent=p.b;d.body.appendChild(b);var j=d.createElement("script");j.textContent=p.j;d.body.appendChild(j)})</script>""";

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

        return ComposeSelfExtractingHtml(
            css,
            js,
            HtmlBundleBootstrapSupport.BuildRawBootstrapJson(
                prepared,
                options.IncludeHidden,
                options.IncludeMetadata,
                features,
                label,
                title,
                commandPrefix,
                themeOptions));
    }

    public static async Task<string> BuildSelfExtractingHtmlAsync(
        AcquiredRenderDocument prepared,
        RenderExecutionOptions options,
        HtmlFeatureFlags features,
        string? label,
        string? title,
        string? commandPrefix,
        HtmlThemeOptions? themeOptions,
        string bundleRoot,
        CancellationToken cancellationToken)
    {
        var staticHtml = await File.ReadAllTextAsync(Path.Combine(bundleRoot, "static.html"), cancellationToken);
        var cssBuilder = new List<string>();
        foreach (var path in EnumerateLinkedAssetPaths(staticHtml, "link", ".css"))
        {
            var css = await TryReadTextAsync(ResolveBundleAssetPath(bundleRoot, path), cancellationToken);
            if (css is not null)
            {
                cssBuilder.Add(css);
            }
        }

        var jsBuilder = new List<string>();
        foreach (var path in EnumerateLinkedAssetPaths(staticHtml, "script", ".js"))
        {
            var script = await BuildInlineScriptAsync(ResolveBundleAssetPath(bundleRoot, path), cancellationToken);
            if (script is not null)
            {
                jsBuilder.Add(script);
            }
        }

        return ComposeSelfExtractingHtml(
            string.Join("\n", cssBuilder),
            string.Join("\n", jsBuilder),
            HtmlBundleBootstrapSupport.BuildRawBootstrapJson(
                prepared,
                options.IncludeHidden,
                options.IncludeMetadata,
                features,
                label,
                title,
                commandPrefix,
                themeOptions));
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
            return !File.Exists(entryPath)
                ? match.Value
                : $"<script>{BuildInlineScript(entryPath)}</script>";
        });

        return html;
    }

    public static async Task<string> InlineAssetsAsync(
        string html,
        string bundleRoot,
        CancellationToken cancellationToken)
    {
        html = await InlineStylesAsync(html, bundleRoot, cancellationToken);
        html = Regex.Replace(html, @"<link\s[^>]*rel=""modulepreload""[^>]*/?>[\r\n]*", string.Empty);
        return await InlineScriptsAsync(html, bundleRoot, cancellationToken);
    }

    public static async Task<HashSet<string>> CollectReferencedAssetsAsync(
        string bundleRoot,
        CancellationToken cancellationToken)
    {
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "static.html" };
        var staticHtmlPath = Path.Combine(bundleRoot, "static.html");
        string html;
        try
        {
            html = await File.ReadAllTextAsync(staticHtmlPath, cancellationToken);
        }
        catch (DirectoryNotFoundException)
        {
            return referenced;
        }
        catch (FileNotFoundException)
        {
            return referenced;
        }

        AddReferencedAssets(referenced, html);
        HtmlBundleAssetValidation.AssertReferencedAssetsExist(bundleRoot, referenced);
        return referenced;
    }

    private static void AddReferencedAssets(ISet<string> referenced, string html)
    {
        foreach (Match match in Regex.Matches(html, @"(?:src|href)=""\./([^""]+)"""))
        {
            referenced.Add(match.Groups[1].Value);
        }
    }

    private static string ComposeSelfExtractingHtml(string css, string js, string bootstrap)
    {
        var pack = JsonSerializer.Serialize(new { c = css, j = js, b = bootstrap }, CompactSerializerOptions);
        var compressedBlob = HtmlBundleCompression.GzipBase64(pack);
        return SelfExtractingHead
            + SelfExtractingThemeScript
            + """<script id="_z" type="text/plain">""" + compressedBlob + "</script>"
            + SelfExtractingDecompressor
            + "</body></html>";
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

    private static string ResolveBundleAssetPath(string bundleRoot, string relativeAssetPath)
        => Path.Combine(bundleRoot, relativeAssetPath.Replace('/', Path.DirectorySeparatorChar));

    private static string BuildInlineScript(string entryPath)
    {
        var entryCode = File.ReadAllText(entryPath);
        var entryDirectory = Path.GetDirectoryName(entryPath)!;
        return HtmlBundleModuleSupport.BundleModulesAsIife(entryCode, entryDirectory);
    }

    private static async Task<string> InlineStylesAsync(
        string html,
        string bundleRoot,
        CancellationToken cancellationToken)
    {
        var pattern = @"<link\s[^>]*href=""\./(assets/[^""]+\.css)""[^>]*/?>";
        var matches = Regex.Matches(html, pattern);
        for (var index = matches.Count - 1; index >= 0; index--)
        {
            var match = matches[index];
            var cssPath = ResolveBundleAssetPath(bundleRoot, match.Groups[1].Value);
            var css = await TryReadTextAsync(cssPath, cancellationToken);
            if (css is null)
            {
                continue;
            }

            html = html.Remove(match.Index, match.Length).Insert(match.Index, $"<style>{css}</style>");
        }

        return html;
    }

    private static async Task<string> InlineScriptsAsync(
        string html,
        string bundleRoot,
        CancellationToken cancellationToken)
    {
        var pattern = @"<script\s[^>]*src=""\./(assets/[^""]+\.js)""[^>]*></script>[\r\n]*";
        var matches = Regex.Matches(html, pattern);
        for (var index = matches.Count - 1; index >= 0; index--)
        {
            var match = matches[index];
            var entryPath = ResolveBundleAssetPath(bundleRoot, match.Groups[1].Value);
            var script = await BuildInlineScriptAsync(entryPath, cancellationToken);
            if (script is null)
            {
                continue;
            }

            html = html.Remove(match.Index, match.Length).Insert(match.Index, $"<script>{script}</script>");
        }

        return html;
    }

    private static async Task<string?> BuildInlineScriptAsync(string entryPath, CancellationToken cancellationToken)
    {
        string entryCode;
        try
        {
            entryCode = await File.ReadAllTextAsync(entryPath, cancellationToken);
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
        catch (FileNotFoundException)
        {
            return null;
        }

        var entryDirectory = Path.GetDirectoryName(entryPath)!;
        return await HtmlBundleModuleSupport.BundleModulesAsIifeAsync(entryCode, entryDirectory, cancellationToken);
    }

    private static async Task<string?> TryReadTextAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            return await File.ReadAllTextAsync(path, cancellationToken);
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}
