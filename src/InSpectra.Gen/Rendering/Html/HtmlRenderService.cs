using InSpectra.Gen.Models;
using InSpectra.Gen.Acquisition.Runtime;
using InSpectra.Gen.Runtime.Rendering;

namespace InSpectra.Gen.Rendering.Html;

public sealed class HtmlRenderService(
    IDocumentRenderService documentService,
    OpenCliNormalizer normalizer,
    IViewerBundleLocator bundleLocator,
    RenderStatsFactory statsFactory)
{
    private const string BootstrapPlaceholder = "__INSPECTRA_BOOTSTRAP__";
    private sealed record BundleFile(string SourcePath, string RelativePath);

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
        var bundleRoot = await bundleLocator.ResolveAsync(cancellationToken, allowBuild: !options.DryRun);
        var bundleFiles = Directory.EnumerateFiles(bundleRoot, "*", SearchOption.AllDirectories)
            .Select(path => new BundleFile(path, Path.GetRelativePath(bundleRoot, path).Replace('\\', '/')))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToList();

        // Only ship assets that static.html actually references — skip website-only files
        var referencedAssets = HtmlBundleComposer.CollectReferencedAssets(bundleRoot);

        if (options.DryRun)
        {
            var plannedFiles = CreatePlannedFiles(bundleFiles, referencedAssets, outputDirectory, options.SingleFile);

            return CreateResult(
                prepared,
                normalized,
                options,
                plannedFiles,
                $"Dry run: render `{prepared.Source.OpenCliOrigin}` as an HTML app bundle in `{outputDirectory}` ({plannedFiles.Count} files planned).");
        }

        OutputPathHelper.PrepareDirectory(outputDirectory, options.Overwrite);

        var bootstrapJson = HtmlBundleComposer.BuildInlineBootstrap(
            prepared,
            options.IncludeHidden,
            options.IncludeMetadata,
            features,
            label,
            title,
            commandPrefix,
            themeOptions,
            options.CompressLevel);
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
                if (options.CompressLevel >= 1)
                {
                    html = HtmlBundleComposer.MinifyHtml(html);
                }
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
                ? HtmlBundleComposer.BuildSelfExtractingHtml(
                    prepared,
                    options,
                    features,
                    label,
                    title,
                    commandPrefix,
                    themeOptions,
                    bundleRoot,
                    outputDirectory)
                : HtmlBundleComposer.InlineAssets(
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

    private static List<RenderedFile> CreatePlannedFiles(
        IReadOnlyList<BundleFile> bundleFiles,
        ISet<string> referencedAssets,
        string outputDirectory,
        bool singleFile)
    {
        if (singleFile)
        {
            var indexPath = Path.Combine(outputDirectory, "index.html");
            return [new RenderedFile("index.html", indexPath, null)];
        }

        var plannedFiles = new List<RenderedFile>();
        foreach (var file in bundleFiles)
        {
            if (!referencedAssets.Contains(file.RelativePath))
            {
                continue;
            }

            var relativePath = string.Equals(file.RelativePath, "static.html", StringComparison.OrdinalIgnoreCase)
                ? "index.html"
                : file.RelativePath;
            plannedFiles.Add(new RenderedFile(relativePath, Path.Combine(outputDirectory, relativePath), null));
        }

        return plannedFiles;
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
            Acquisition = prepared.Acquisition,
            Warnings = prepared.Warnings,
            IsDryRun = options.DryRun,
            StdoutDocument = null,
            Files = files,
            Summary = summary,
            Stats = statsFactory.Create(normalized, files.Count),
        };
    }
}
