using InSpectra.Lib;
using InSpectra.Lib.Rendering.Contracts;
using InSpectra.Lib.Rendering.Html.Bundle;
using InSpectra.Lib.Rendering.Pipeline;
using InSpectra.Lib.Rendering.Pipeline.Model;

namespace InSpectra.Lib.Rendering.Html;

internal sealed class HtmlRenderService(
    IDocumentRenderService documentService,
    OpenCliNormalizer normalizer,
    IViewerBundleLocator bundleLocator,
    RenderStatsFactory statsFactory)
    : IHtmlRenderService
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
            ?? throw new CliUsageException("HTML output requires an output directory.");
        var normalized = normalizer.Normalize(prepared.RawDocument, options.IncludeHidden);
        var bundleRoot = await bundleLocator.ResolveAsync(cancellationToken, allowBuild: !options.DryRun);
        var bundleFiles = options.SingleFile && !options.DryRun
            ? []
            : Directory.EnumerateFiles(bundleRoot, "*", SearchOption.AllDirectories)
                .Select(path => new BundleFile(path, Path.GetRelativePath(bundleRoot, path).Replace('\\', '/')))
                .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                .ToList();

        // Only ship assets that static.html actually references — skip website-only files
        var referencedAssets = await HtmlBundleAssetComposer.CollectReferencedAssetsAsync(bundleRoot, cancellationToken);

        if (options.DryRun)
        {
            var plannedFiles = CreatePlannedFiles(bundleFiles, referencedAssets, outputDirectory, options.SingleFile);
            return CreateResult(
                prepared,
                normalized,
                options,
                plannedFiles);
        }

        var writtenFiles = await OutputPathHelper.PublishDirectoryAsync(
            outputDirectory,
            options.Overwrite,
            async (stagingDirectory, token) =>
            {
                if (options.SingleFile)
                {
                    return await RenderSingleFileAsync(
                        prepared,
                        options,
                        features,
                        label,
                        title,
                        commandPrefix,
                        themeOptions,
                        bundleRoot,
                        stagingDirectory,
                        outputDirectory,
                        token);
                }

                return await RenderBundleAsync(
                    prepared,
                    options,
                    features,
                    label,
                    title,
                    commandPrefix,
                    themeOptions,
                    bundleFiles,
                    referencedAssets,
                    stagingDirectory,
                    outputDirectory,
                    token);
            },
            cancellationToken);
        return CreateResult(prepared, normalized, options, writtenFiles);
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
        IReadOnlyList<RenderedFile> files)
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
            Stats = statsFactory.Create(normalized, files.Count),
        };
    }

    private async Task<List<RenderedFile>> RenderSingleFileAsync(
        AcquiredRenderDocument prepared,
        RenderExecutionOptions options,
        HtmlFeatureFlags features,
        string? label,
        string? title,
        string? commandPrefix,
        HtmlThemeOptions? themeOptions,
        string bundleRoot,
        string stagingOutputDirectory,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        string html;
        if (options.CompressLevel >= 2)
        {
            html = await HtmlBundleAssetComposer.BuildSelfExtractingHtmlAsync(
                prepared,
                options,
                features,
                label,
                title,
                commandPrefix,
                themeOptions,
                bundleRoot,
                cancellationToken);
        }
        else
        {
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
            var indexTemplatePath = Path.Combine(bundleRoot, "static.html");
            var indexHtml = await BuildIndexHtmlAsync(indexTemplatePath, bootstrapJson, options.CompressLevel, cancellationToken);
            html = await HtmlBundleAssetComposer.InlineAssetsAsync(indexHtml, bundleRoot, cancellationToken);
        }

        var indexDestination = Path.Combine(stagingOutputDirectory, "index.html");
        await File.WriteAllTextAsync(indexDestination, html, cancellationToken);
        return [new RenderedFile("index.html", Path.Combine(outputDirectory, "index.html"), html)];
    }

    private async Task<List<RenderedFile>> RenderBundleAsync(
        AcquiredRenderDocument prepared,
        RenderExecutionOptions options,
        HtmlFeatureFlags features,
        string? label,
        string? title,
        string? commandPrefix,
        HtmlThemeOptions? themeOptions,
        IReadOnlyList<BundleFile> bundleFiles,
        ISet<string> referencedAssets,
        string stagingOutputDirectory,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
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
            cancellationToken.ThrowIfCancellationRequested();

            if (!referencedAssets.Contains(file.RelativePath))
            {
                continue;
            }

            if (string.Equals(file.RelativePath, "static.html", StringComparison.OrdinalIgnoreCase))
            {
                var html = await BuildIndexHtmlAsync(file.SourcePath, bootstrapJson, options.CompressLevel, cancellationToken);
                var indexDestination = Path.Combine(stagingOutputDirectory, "index.html");
                await File.WriteAllTextAsync(indexDestination, html, cancellationToken);
                writtenFiles.Add(new RenderedFile("index.html", Path.Combine(outputDirectory, "index.html"), html));
                continue;
            }

            var destinationPath = Path.Combine(stagingOutputDirectory, file.RelativePath);
            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            await CopyFileAsync(file.SourcePath, destinationPath, cancellationToken);
            writtenFiles.Add(new RenderedFile(file.RelativePath, Path.Combine(outputDirectory, file.RelativePath), null));
        }

        return writtenFiles;
    }

    private static async Task<string> BuildIndexHtmlAsync(
        string templatePath,
        string bootstrapJson,
        int compressLevel,
        CancellationToken cancellationToken)
    {
        var html = await File.ReadAllTextAsync(templatePath, cancellationToken);
        html = html.Replace(BootstrapPlaceholder, bootstrapJson, StringComparison.Ordinal);
        return compressLevel >= 1
            ? HtmlBundleComposer.MinifyHtml(html)
            : html;
    }

    private static async Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        await using var source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous);
        await using var destination = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous);
        await source.CopyToAsync(destination, cancellationToken);
    }
}
