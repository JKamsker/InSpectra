using InSpectra.Gen.Rendering.Pipeline.Model;
using InSpectra.Gen.Core;
using InSpectra.Gen.Rendering.Contracts;

namespace InSpectra.Gen.Rendering.Markdown;

public sealed class MarkdownRenderService(
    IDocumentRenderService documentService,
    OpenCliNormalizer normalizer,
    MarkdownRenderer renderer,
    RenderStatsFactory statsFactory)
{
    public async Task<RenderExecutionResult> RenderFromFileAsync(
        FileRenderRequest request,
        CancellationToken cancellationToken)
    {
        var prepared = await documentService.LoadFromFileAsync(request, cancellationToken);
        return await RenderAsync(prepared, request.Options, request.MarkdownOptions, cancellationToken);
    }

    private Task<RenderExecutionResult> RenderAsync(
        AcquiredRenderDocument prepared,
        RenderExecutionOptions options,
        MarkdownRenderOptions? markdownOptions,
        CancellationToken cancellationToken)
    {
        var normalized = normalizer.Normalize(prepared.RenderDocument, options.IncludeHidden);

        return options.Layout switch
        {
            RenderLayout.Single => HandleSingleLayoutAsync(prepared, normalized, options, markdownOptions, cancellationToken),
            RenderLayout.Tree => HandleTreeLayoutAsync(prepared, normalized, options, markdownOptions, cancellationToken),
            RenderLayout.Hybrid => HandleHybridLayoutAsync(prepared, normalized, options, markdownOptions, cancellationToken),
            _ => throw new CliUsageException("Markdown rendering supports `single`, `tree`, and `hybrid` layouts only."),
        };
    }

    private async Task<RenderExecutionResult> HandleHybridLayoutAsync(
        AcquiredRenderDocument prepared,
        NormalizedCliDocument document,
        RenderExecutionOptions options,
        MarkdownRenderOptions? markdownOptions,
        CancellationToken cancellationToken)
    {
        var outputDirectory = options.OutputDirectory
            ?? throw new CliUsageException("`--layout hybrid` requires `--out-dir`.");
        var splitDepth = markdownOptions?.HybridSplitDepth ?? 1;
        var files = renderer.RenderHybrid(document, options.IncludeMetadata, splitDepth, markdownOptions);

        if (options.DryRun)
        {
            var planned = files
                .Select(file => new RenderedFile(file.RelativePath, Path.Combine(outputDirectory, file.RelativePath), null))
                .ToList();

            return CreateResult(
                prepared,
                document,
                options,
                planned,
                $"Dry run: render `{prepared.Source.OpenCliOrigin}` as hybrid Markdown in `{outputDirectory}` ({planned.Count} files planned).");
        }

        OutputPathHelper.PrepareDirectory(outputDirectory, options.Overwrite);

        var writtenFiles = await WriteRenderedFilesAsync(outputDirectory, files, cancellationToken);
        var summary = options.Quiet ? null : $"Wrote {writtenFiles.Count} hybrid Markdown files to `{outputDirectory}`.";
        return CreateResult(prepared, document, options, writtenFiles, summary);
    }

    private async Task<RenderExecutionResult> HandleSingleLayoutAsync(
        AcquiredRenderDocument prepared,
        NormalizedCliDocument document,
        RenderExecutionOptions options,
        MarkdownRenderOptions? markdownOptions,
        CancellationToken cancellationToken)
    {
        var content = renderer.RenderSingle(document, options.IncludeMetadata, markdownOptions);

        if (options.DryRun)
        {
            IReadOnlyList<RenderedFile> plannedFiles = options.OutputFile is null
                ? []
                : [new RenderedFile(Path.GetFileName(options.OutputFile), options.OutputFile, null)];

            return CreateResult(
                prepared,
                document,
                options,
                plannedFiles,
                options.OutputFile is null
                    ? $"Dry run: render `{prepared.Source.OpenCliOrigin}` as single Markdown to stdout."
                    : $"Dry run: render `{prepared.Source.OpenCliOrigin}` as single Markdown to `{options.OutputFile}`.");
        }

        if (options.OutputFile is null)
        {
            return CreateResult(prepared, document, options, [], summary: null, stdoutDocument: content);
        }

        OutputPathHelper.EnsureFileWritable(options.OutputFile, options.Overwrite);
        var directory = Path.GetDirectoryName(options.OutputFile);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(options.OutputFile, content, cancellationToken);

        var written = new RenderedFile(Path.GetFileName(options.OutputFile), options.OutputFile, content);
        var summary = options.Quiet ? null : $"Wrote Markdown to `{options.OutputFile}`.";
        return CreateResult(prepared, document, options, [written], summary);
    }

    private async Task<RenderExecutionResult> HandleTreeLayoutAsync(
        AcquiredRenderDocument prepared,
        NormalizedCliDocument document,
        RenderExecutionOptions options,
        MarkdownRenderOptions? markdownOptions,
        CancellationToken cancellationToken)
    {
        var outputDirectory = options.OutputDirectory
            ?? throw new CliUsageException("`--layout tree` requires `--out-dir`.");
        var files = renderer.RenderTree(document, options.IncludeMetadata, markdownOptions);

        if (options.DryRun)
        {
            var planned = files
                .Select(file => new RenderedFile(file.RelativePath, Path.Combine(outputDirectory, file.RelativePath), null))
                .ToList();

            return CreateResult(
                prepared,
                document,
                options,
                planned,
                $"Dry run: render `{prepared.Source.OpenCliOrigin}` as a Markdown tree in `{outputDirectory}` ({planned.Count} files planned).");
        }

        OutputPathHelper.PrepareDirectory(outputDirectory, options.Overwrite);

        var writtenFiles = await WriteRenderedFilesAsync(outputDirectory, files, cancellationToken);
        var summary = options.Quiet ? null : $"Wrote {writtenFiles.Count} Markdown files to `{outputDirectory}`.";
        return CreateResult(prepared, document, options, writtenFiles, summary);
    }

    private static async Task<List<RenderedFile>> WriteRenderedFilesAsync(
        string outputDirectory,
        IReadOnlyList<RelativeRenderedFile> files,
        CancellationToken cancellationToken)
    {
        var writtenFiles = new List<RenderedFile>();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullPath = Path.Combine(outputDirectory, file.RelativePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, file.Content, cancellationToken);
            writtenFiles.Add(new RenderedFile(file.RelativePath, fullPath, file.Content));
        }

        return writtenFiles;
    }

    private RenderExecutionResult CreateResult(
        AcquiredRenderDocument prepared,
        NormalizedCliDocument document,
        RenderExecutionOptions options,
        IReadOnlyList<RenderedFile> files,
        string? summary,
        string? stdoutDocument = null)
    {
        return new RenderExecutionResult
        {
            Format = DocumentFormat.Markdown,
            Layout = options.Layout,
            Source = prepared.Source,
            Acquisition = prepared.Acquisition,
            Warnings = prepared.Warnings,
            IsDryRun = options.DryRun,
            StdoutDocument = stdoutDocument,
            Files = files,
            Summary = summary,
            Stats = statsFactory.Create(document, files.Count),
        };
    }
}
