using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using InSpectra.Gen.Models;
using InSpectra.Gen.Runtime;

namespace InSpectra.Gen.Services;

public sealed class HtmlRenderService(
    DocumentRenderService documentService,
    OpenCliNormalizer normalizer,
    ViewerBundleLocator bundleLocator,
    RenderStatsFactory statsFactory)
{
    private const string BootstrapPlaceholder = "__INSPECTRA_BOOTSTRAP__";

    public async Task<RenderExecutionResult> RenderFromFileAsync(
        FileRenderRequest request,
        HtmlFeatureFlags features,
        CancellationToken cancellationToken,
        string? label = null,
        string? title = null,
        string? commandPrefix = null,
        HtmlThemeOptions? themeOptions = null)
    {
        var prepared = await documentService.LoadFromFileAsync(request, cancellationToken);
        return await RenderAsync(prepared, request.Options, features, label, title, commandPrefix, themeOptions, cancellationToken);
    }

    public async Task<RenderExecutionResult> RenderFromExecAsync(
        ExecRenderRequest request,
        HtmlFeatureFlags features,
        CancellationToken cancellationToken,
        string? label = null,
        string? title = null,
        string? commandPrefix = null,
        HtmlThemeOptions? themeOptions = null)
    {
        var prepared = await documentService.LoadFromExecAsync(request, cancellationToken);
        return await RenderAsync(prepared, request.Options, features, label, title, commandPrefix, themeOptions, cancellationToken);
    }

    private async Task<RenderExecutionResult> RenderAsync(
        AcquiredRenderDocument prepared,
        RenderExecutionOptions options,
        HtmlFeatureFlags features,
        string? label,
        string? title,
        string? commandPrefix,
        HtmlThemeOptions? themeOptions,
        CancellationToken cancellationToken)
    {
        var outputDirectory = options.OutputDirectory
            ?? throw new CliUsageException("HTML output requires `--out-dir`.");
        var normalized = normalizer.Normalize(prepared.RawDocument, options.IncludeHidden);
        var bundleRoot = await bundleLocator.ResolveAsync(cancellationToken);
        var bundleFiles = Directory.EnumerateFiles(bundleRoot, "*", SearchOption.AllDirectories)
            .Select(path => new
            {
                SourcePath = path,
                RelativePath = Path.GetRelativePath(bundleRoot, path).Replace('\\', '/'),
            })
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToList();

        // Only ship assets that static.html actually references — skip website-only files
        var referencedAssets = CollectReferencedAssets(bundleRoot);

        if (options.DryRun)
        {
            var plannedFiles = bundleFiles
                .Where(file => referencedAssets.Contains(file.RelativePath))
                .Select(file => new RenderedFile(file.RelativePath, Path.Combine(outputDirectory, file.RelativePath), null))
                .ToList();

            return CreateResult(
                prepared,
                normalized,
                options,
                plannedFiles,
                $"Dry run: render `{prepared.Source.OpenCliOrigin}` as an HTML app bundle in `{outputDirectory}` ({plannedFiles.Count} files planned).");
        }

        OutputPathHelper.PrepareDirectory(outputDirectory, options.Overwrite);

        var bootstrapJson = BuildInlineBootstrap(prepared, options.IncludeHidden, options.IncludeMetadata, features, label, title, commandPrefix, themeOptions, options.CompressLevel);
        var writtenFiles = new List<RenderedFile>();
        foreach (var file in bundleFiles)
        {
            // Skip assets not referenced by the static bundle template
            if (!referencedAssets.Contains(file.RelativePath))
            {
                continue;
            }

            // static.html is the static bundle template — inject bootstrap and write as index.html
            if (string.Equals(file.RelativePath, "static.html", StringComparison.OrdinalIgnoreCase))
            {
                var html = await File.ReadAllTextAsync(file.SourcePath, cancellationToken);
                html = html.Replace(BootstrapPlaceholder, bootstrapJson, StringComparison.Ordinal);
                if (options.CompressLevel >= 1) html = MinifyHtml(html);
                var indexDestination = Path.Combine(outputDirectory, "index.html");
                await File.WriteAllTextAsync(indexDestination, html, cancellationToken);
                writtenFiles.Add(new RenderedFile("index.html", indexDestination, html));
                continue;
            }

            var destinationPath = Path.Combine(outputDirectory, file.RelativePath);
            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(file.SourcePath, destinationPath, overwrite: true);
            writtenFiles.Add(new RenderedFile(file.RelativePath, destinationPath, null));
        }

        if (options.SingleFile)
        {
            var selfExtractingHtml = options.CompressLevel >= 2
                ? BuildSelfExtractingHtml(prepared, options, features, label, title, commandPrefix, themeOptions, outputDirectory)
                : InlineAssets(
                    writtenFiles.First(f => f.RelativePath == "index.html").Content!,
                    outputDirectory);

            // Remove all written files — we're replacing with a single self-extracting HTML
            foreach (var file in writtenFiles)
            {
                if (File.Exists(file.FullPath))
                {
                    File.Delete(file.FullPath);
                }
            }

            var assetsDir = Path.Combine(outputDirectory, "assets");
            if (Directory.Exists(assetsDir) && !Directory.EnumerateFileSystemEntries(assetsDir).Any())
            {
                Directory.Delete(assetsDir);
            }

            var indexDestination = Path.Combine(outputDirectory, "index.html");
            await File.WriteAllTextAsync(indexDestination, selfExtractingHtml, cancellationToken);
            writtenFiles = [new RenderedFile("index.html", indexDestination, selfExtractingHtml)];
        }

        var summary = options.Quiet
            ? null
            : $"Wrote HTML app bundle ({writtenFiles.Count} files) to `{outputDirectory}`.";
        return CreateResult(prepared, normalized, options, writtenFiles, summary);
    }

    private RenderExecutionResult CreateResult(
        AcquiredRenderDocument prepared,
        NormalizedCliDocument normalized,
        RenderExecutionOptions options,
        IReadOnlyList<RenderedFile> files,
        string? summary)
    {
        return new RenderExecutionResult
        {
            Format = DocumentFormat.Html,
            Layout = RenderLayout.App,
            Source = prepared.Source,
            Warnings = prepared.Warnings,
            IsDryRun = options.DryRun,
            StdoutDocument = null,
            Files = files,
            Summary = summary,
            Stats = statsFactory.Create(normalized, files.Count),
        };
    }

    /// <summary>
    /// Builds a self-extracting HTML file that packs CSS, JS, and bootstrap data into a
    /// single gzip+base64 blob. A tiny inline decompressor unpacks everything at load time.
    /// </summary>
    private string BuildSelfExtractingHtml(
        AcquiredRenderDocument prepared,
        RenderExecutionOptions options,
        HtmlFeatureFlags features,
        string? label,
        string? title,
        string? commandPrefix,
        HtmlThemeOptions? themeOptions,
        string outputDirectory)
    {
        // Collect assets
        var referencedAssets = CollectReferencedAssets(
            bundleLocator.ResolveAsync(CancellationToken.None).GetAwaiter().GetResult());

        var assetsDir = Path.Combine(outputDirectory, "assets");
        var cssFiles = Directory.GetFiles(assetsDir, "*.css")
            .Where(f => referencedAssets.Contains("assets/" + Path.GetFileName(f)))
            .ToList();
        var jsEntryFiles = Directory.GetFiles(assetsDir, "*.js")
            .Where(f => referencedAssets.Contains("assets/" + Path.GetFileName(f)))
            .ToList();

        var css = string.Join("\n", cssFiles.Select(File.ReadAllText));

        // Build the IIFE JS bundle from entry + chunks
        var entryFile = jsEntryFiles.FirstOrDefault(f => !Path.GetFileName(f).StartsWith("loadSource", StringComparison.Ordinal));
        var js = entryFile is not null
            ? BundleModulesAsIife(File.ReadAllText(entryFile), assetsDir)
            : string.Join("\n", jsEntryFiles.Select(File.ReadAllText));

        // Build raw bootstrap JSON (not pre-compressed — we'll compress everything together)
        var bootstrapPayload = BuildRawBootstrapJson(prepared, options.IncludeHidden, options.IncludeMetadata, features, label, title, commandPrefix, themeOptions);

        // Pack all payloads into one JSON object and gzip+base64 the whole thing
        var pack = JsonSerializer.Serialize(new { c = css, j = js, b = bootstrapPayload }, JsonOutput.CompactSerializerOptions);
        var compressedBlob = GzipBase64(pack);

        // Build the minimal self-extracting HTML
        const string head =
            """<!doctype html><html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0"><title>InSpectraUI</title><link rel="preconnect" href="https://fonts.googleapis.com"><link rel="preconnect" href="https://fonts.gstatic.com" crossorigin><link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500;700&display=swap" rel="stylesheet"></head><body><div id="root"></div>""";

        const string themeScript =
            """<script>(function(){var s=localStorage.getItem("inspectra-theme");if(s==="dark"||s==="light")document.documentElement.dataset.theme=s;else if(matchMedia("(prefers-color-scheme:dark)").matches)document.documentElement.dataset.theme="dark";var c=localStorage.getItem("inspectra-color-theme");if(c)document.documentElement.dataset.colorTheme=c})()</script>""";

        const string decompressor =
            """<script>var _u=Uint8Array.from(atob(document.getElementById("_z").textContent),function(c){return c.charCodeAt(0)});new Response(new Blob([_u]).stream().pipeThrough(new DecompressionStream("gzip"))).text().then(function(t){var p=JSON.parse(t);var d=document;var s=d.createElement("style");s.textContent=p.c;d.head.appendChild(s);var b=d.createElement("script");b.id="inspectra-bootstrap";b.type="application/json";b.textContent=p.b;d.body.appendChild(b);var j=d.createElement("script");j.textContent=p.j;d.body.appendChild(j)})</script>""";

        return head + themeScript +
               """<script id="_z" type="text/plain">""" + compressedBlob + "</script>" +
               decompressor + "</body></html>";
    }

    private static string BuildRawBootstrapJson(
        AcquiredRenderDocument prepared,
        bool includeHidden,
        bool includeMetadata,
        HtmlFeatureFlags features,
        string? label,
        string? title,
        string? commandPrefix,
        HtmlThemeOptions? themeOptions = null)
    {
        var payload = new InlineBootstrap
        {
            Mode = "inline",
            OpenCli = prepared.RawDocument,
            XmlDoc = prepared.XmlDocument,
            Options = new ViewerOptionsPayload
            {
                IncludeHidden = includeHidden,
                IncludeMetadata = includeMetadata,
                Label = label,
                Title = title,
                CommandPrefix = commandPrefix,
                Theme = themeOptions?.Theme,
                ColorTheme = themeOptions?.ColorTheme,
                CustomAccent = themeOptions?.CustomAccent,
                CustomAccentDark = themeOptions?.CustomAccentDark,
            },
            Features = new FeatureFlagsPayload
            {
                ShowHome = features.ShowHome,
                Composer = features.Composer,
                DarkTheme = features.DarkTheme,
                LightTheme = features.LightTheme,
                UrlLoading = features.UrlLoading,
                NugetBrowser = features.NugetBrowser,
                PackageUpload = features.PackageUpload,
                ColorThemePicker = features.ColorThemePicker,
            },
        };

        return JsonSerializer.Serialize(payload, JsonOutput.CompactSerializerOptions);
    }

    private static string InlineAssets(string html, string outputDirectory)
    {
        // Inline CSS: <link rel="stylesheet" ... href="./assets/..."> → <style>...</style>
        html = Regex.Replace(html, @"<link\s[^>]*href=""\./(assets/[^""]+\.css)""[^>]*/?>", match =>
        {
            var cssPath = Path.Combine(outputDirectory, match.Groups[1].Value);
            if (!File.Exists(cssPath)) return match.Value;
            var css = File.ReadAllText(cssPath);
            return $"<style>{css}</style>";
        });

        // Remove modulepreload links (the code will be inlined)
        html = Regex.Replace(html, @"<link\s[^>]*rel=""modulepreload""[^>]*/?>[\r\n]*", "");

        // Replace <script type="application/json"> bootstrap with JSON.parse string literal
        // V8 optimizes JSON.parse('...') with a literal argument — faster than textContent + parse
        html = Regex.Replace(
            html,
            @"<script\s+id=""inspectra-bootstrap""\s+type=""application/json"">[^<]+</script>",
            match =>
            {
                var tagContent = Regex.Match(match.Value, @">([^<]+)<").Groups[1].Value;
                var escaped = EscapeForJsStringLiteral(tagContent);
                return $"<script>window.__inspectraBootstrap=JSON.parse('{escaped}')</script>";
            });

        // Inline JS: <script type="module" ... src="./assets/..."> → single inline <script>
        // Remove the script tag from <head> and inject before </body> so the DOM is ready
        string? inlinedScriptBlock = null;
        html = Regex.Replace(html, @"<script\s[^>]*src=""\./(assets/[^""]+\.js)""[^>]*></script>[\r\n]*", match =>
        {
            var entryRelPath = match.Groups[1].Value;
            var entryPath = Path.Combine(outputDirectory, entryRelPath);
            if (!File.Exists(entryPath)) return match.Value;

            var entryCode = File.ReadAllText(entryPath);
            var entryDir = Path.GetDirectoryName(entryPath)!;
            var inlinedJs = BundleModulesAsIife(entryCode, entryDir);
            inlinedScriptBlock = $"<script>{inlinedJs}</script>";
            return ""; // Remove from <head>
        });

        // Insert the script at the end of <body> where #root already exists
        if (inlinedScriptBlock is not null)
        {
            html = html.Replace("</body>", $"{inlinedScriptBlock}\n</body>");
        }

        return html;
    }

    /// <summary>
    /// Escapes a string for safe embedding inside a JS single-quoted string literal.
    /// </summary>
    private static string EscapeForJsStringLiteral(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("</script", "<\\/script", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Takes an ES module entry script that imports from sibling chunks and produces
    /// a single IIFE that can run as a classic (non-module) script from file://.
    /// </summary>
    private static string BundleModulesAsIife(string entryCode, string entryDirectory)
    {
        // Parse: import{X as a, Y as b}from"./chunk.js";
        // The import path is relative to the entry script, which lives in assets/
        var importPattern = new Regex(@"import\{([^}]+)\}from""\./([\w.=-]+\.js)"";?");
        var importMatch = importPattern.Match(entryCode);

        if (!importMatch.Success)
        {
            // No imports — just wrap in IIFE
            return $"(function(){{{entryCode}}})();";
        }

        var chunkFileName = importMatch.Groups[2].Value;
        var chunkPath = Path.Combine(entryDirectory, chunkFileName);
        if (!File.Exists(chunkPath))
        {
            return $"(function(){{{entryCode}}})();";
        }

        var chunkCode = File.ReadAllText(chunkPath);

        // Parse the chunk's export statement: export{localVar as ExportedName, ...};
        var exportPattern = new Regex(@"export\{([^}]+)\};?\s*$");
        var exportMatch = exportPattern.Match(chunkCode);

        if (!exportMatch.Success)
        {
            return $"(function(){{{chunkCode}\n{entryCode}}})();";
        }

        // Build export map: ExportedName → localVariable
        var exportMap = new Dictionary<string, string>();
        foreach (var pair in exportMatch.Groups[1].Value.Split(','))
        {
            var parts = pair.Trim().Split(" as ", 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                exportMap[parts[1]] = parts[0]; // exportedName → localVar
            }
            else if (parts.Length == 1)
            {
                exportMap[parts[0]] = parts[0]; // same name
            }
        }

        // Parse the entry's import bindings: ExportedName as localAlias
        var importBindings = new List<(string exportedName, string localAlias)>();
        foreach (var pair in importMatch.Groups[1].Value.Split(','))
        {
            var parts = pair.Trim().Split(" as ", 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                importBindings.Add((parts[0], parts[1]));
            }
            else if (parts.Length == 1)
            {
                importBindings.Add((parts[0], parts[0]));
            }
        }

        // Remove the export statement from the chunk
        var cleanedChunk = exportPattern.Replace(chunkCode, "");

        // Remove the import statement from the entry
        var cleanedEntry = importPattern.Replace(entryCode, "", 1);

        // Build alias assignments: var entryLocal = chunkLocal;
        var aliases = new System.Text.StringBuilder();
        foreach (var (exportedName, localAlias) in importBindings)
        {
            if (exportMap.TryGetValue(exportedName, out var chunkLocal))
            {
                aliases.Append($"var {localAlias}={chunkLocal};");
            }
        }

        // Wrap everything in a scoped IIFE to avoid naming conflicts
        // The chunk runs first (defining its variables), then aliases bridge to the entry's names
        return $"(function(){{var __M=(function(){{" +
               $"{cleanedChunk}" +
               $"return{{{string.Join(",", importBindings.Select(b => exportMap.TryGetValue(b.exportedName, out var v) ? $"{b.exportedName}:{v}" : ""))}}}" +
               $"}})();" +
               $"{string.Join("", importBindings.Select(b => $"var {b.localAlias}=__M.{b.exportedName};"))}" +
               $"{cleanedEntry}}})();";
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

    /// <summary>
    /// Collapse inter-tag whitespace in the HTML template. Preserves content inside
    /// &lt;script&gt;, &lt;style&gt;, and &lt;pre&gt; blocks.
    /// </summary>
    private static string MinifyHtml(string html)
    {
        // Collapse runs of whitespace between > and < to a single space
        html = Regex.Replace(html, @">\s+<", "> <");
        // Remove leading whitespace on each line (indentation)
        html = Regex.Replace(html, @"^\s+", "", RegexOptions.Multiline);
        // Collapse blank lines
        html = Regex.Replace(html, @"\n{2,}", "\n");
        return html.Trim();
    }

    private static HashSet<string> CollectReferencedAssets(string bundleRoot)
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

    private static string BuildInlineBootstrap(
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
        var json = BuildRawBootstrapJson(prepared, includeHidden, includeMetadata, features, label, title, commandPrefix, themeOptions);

        if (compressLevel >= 1)
        {
            return GzipBase64(json);
        }

        // Level 0: raw JSON, escape </script to avoid breaking out of the tag
        return json.Replace("</script", "<\\/script", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class InlineBootstrap
    {
        public required string Mode { get; init; }

        public required OpenCliDocument OpenCli { get; init; }

        public string? XmlDoc { get; init; }

        public required ViewerOptionsPayload Options { get; init; }

        public required FeatureFlagsPayload Features { get; init; }
    }

    private sealed class ViewerOptionsPayload
    {
        public required bool IncludeHidden { get; init; }

        public required bool IncludeMetadata { get; init; }

        public string? Label { get; init; }

        public string? Title { get; init; }

        public string? CommandPrefix { get; init; }

        public string? Theme { get; init; }

        public string? ColorTheme { get; init; }

        public string? CustomAccent { get; init; }

        public string? CustomAccentDark { get; init; }
    }

    private sealed class FeatureFlagsPayload
    {
        public required bool ShowHome { get; init; }

        public required bool Composer { get; init; }

        public required bool DarkTheme { get; init; }

        public required bool LightTheme { get; init; }

        public required bool UrlLoading { get; init; }

        public required bool NugetBrowser { get; init; }

        public required bool PackageUpload { get; init; }

        public required bool ColorThemePicker { get; init; }
    }
}
