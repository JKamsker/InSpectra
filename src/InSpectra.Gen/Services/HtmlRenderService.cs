using System.Text.Json;
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
        string? label = null)
    {
        var prepared = await documentService.LoadFromFileAsync(request, cancellationToken);
        return await RenderAsync(prepared, request.Options, features, label, cancellationToken);
    }

    public async Task<RenderExecutionResult> RenderFromExecAsync(
        ExecRenderRequest request,
        HtmlFeatureFlags features,
        CancellationToken cancellationToken,
        string? label = null)
    {
        var prepared = await documentService.LoadFromExecAsync(request, cancellationToken);
        return await RenderAsync(prepared, request.Options, features, label, cancellationToken);
    }

    private async Task<RenderExecutionResult> RenderAsync(
        AcquiredRenderDocument prepared,
        RenderExecutionOptions options,
        HtmlFeatureFlags features,
        string? label,
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

        if (options.DryRun)
        {
            var plannedFiles = bundleFiles
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

        var bootstrapJson = BuildInlineBootstrap(prepared, options.IncludeHidden, options.IncludeMetadata, features, label);
        var writtenFiles = new List<RenderedFile>();
        foreach (var file in bundleFiles)
        {
            var destinationPath = Path.Combine(outputDirectory, file.RelativePath);
            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            if (string.Equals(file.RelativePath, "index.html", StringComparison.OrdinalIgnoreCase))
            {
                var html = await File.ReadAllTextAsync(file.SourcePath, cancellationToken);
                html = html.Replace(BootstrapPlaceholder, bootstrapJson, StringComparison.Ordinal);
                await File.WriteAllTextAsync(destinationPath, html, cancellationToken);
                writtenFiles.Add(new RenderedFile(file.RelativePath, destinationPath, html));
                continue;
            }

            File.Copy(file.SourcePath, destinationPath, overwrite: true);
            writtenFiles.Add(new RenderedFile(file.RelativePath, destinationPath, null));
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

    private static string BuildInlineBootstrap(
        AcquiredRenderDocument prepared,
        bool includeHidden,
        bool includeMetadata,
        HtmlFeatureFlags features,
        string? label)
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
            },
        };

        return JsonSerializer.Serialize(payload, JsonOutput.SerializerOptions)
            .Replace("</script", "<\\/script", StringComparison.OrdinalIgnoreCase);
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
    }
}
