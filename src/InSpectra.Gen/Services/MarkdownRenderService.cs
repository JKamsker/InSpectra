using InSpectra.Gen.Models;
using InSpectra.Gen.Runtime;

namespace InSpectra.Gen.Services;

public sealed class MarkdownRenderService(
    DocumentRenderService documentService,
    OpenCliNormalizer normalizer,
    MarkdownRenderer renderer,
    RenderStatsFactory statsFactory)
{
    public async Task<RenderExecutionResult> RenderFromFileAsync(
        FileRenderRequest request,
        CancellationToken cancellationToken)
    {
        var prepared = await documentService.LoadFromFileAsync(request, cancellationToken);
        return Render(prepared, request.Options, request.MarkdownOptions);
    }

    public async Task<RenderExecutionResult> RenderFromExecAsync(
        ExecRenderRequest request,
        CancellationToken cancellationToken)
    {
        var prepared = await documentService.LoadFromExecAsync(request, cancellationToken);
        return Render(prepared, request.Options, request.MarkdownOptions);
    }

    private RenderExecutionResult Render(
        AcquiredRenderDocument prepared,
        RenderExecutionOptions options,
        MarkdownRenderOptions? markdownOptions)
    {
        var normalized = normalizer.Normalize(prepared.RenderDocument, options.IncludeHidden);

        return options.Layout switch
        {
            RenderLayout.Single => HandleSingleLayout(prepared, normalized, options, markdownOptions),
            RenderLayout.Tree => HandleTreeLayout(prepared, normalized, options, markdownOptions),
            RenderLayout.Hybrid => HandleHybridLayout(prepared, normalized, options, markdownOptions),
            _ => throw new CliUsageException("Markdown rendering supports `single`, `tree`, and `hybrid` layouts only."),
        };
    }

    private RenderExecutionResult HandleHybridLayout(
        AcquiredRenderDocument prepared,
        NormalizedCliDocument document,
        RenderExecutionOptions options,
        MarkdownRenderOptions? markdownOptions)
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

        var writtenFiles = new List<RenderedFile>();
        foreach (var file in files)
        {
            var fullPath = Path.Combine(outputDirectory, file.RelativePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, file.Content);
            writtenFiles.Add(new RenderedFile(file.RelativePath, fullPath, file.Content));
        }

        var summary = options.Quiet ? null : $"Wrote {writtenFiles.Count} hybrid Markdown files to `{outputDirectory}`.";
        return CreateResult(prepared, document, options, writtenFiles, summary);
    }

    private RenderExecutionResult HandleSingleLayout(
        AcquiredRenderDocument prepared,
        NormalizedCliDocument document,
        RenderExecutionOptions options,
        MarkdownRenderOptions? markdownOptions)
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

        File.WriteAllText(options.OutputFile, content);

        var written = new RenderedFile(Path.GetFileName(options.OutputFile), options.OutputFile, content);
        var summary = options.Quiet ? null : $"Wrote Markdown to `{options.OutputFile}`.";
        return CreateResult(prepared, document, options, [written], summary);
    }

    private RenderExecutionResult HandleTreeLayout(
        AcquiredRenderDocument prepared,
        NormalizedCliDocument document,
        RenderExecutionOptions options,
        MarkdownRenderOptions? markdownOptions)
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

        var writtenFiles = new List<RenderedFile>();
        foreach (var file in files)
        {
            var fullPath = Path.Combine(outputDirectory, file.RelativePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, file.Content);
            writtenFiles.Add(new RenderedFile(file.RelativePath, fullPath, file.Content));
        }

        var summary = options.Quiet ? null : $"Wrote {writtenFiles.Count} Markdown files to `{outputDirectory}`.";
        return CreateResult(prepared, document, options, writtenFiles, summary);
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
            Warnings = prepared.Warnings,
            IsDryRun = options.DryRun,
            StdoutDocument = stdoutDocument,
            Files = files,
            Summary = summary,
            Stats = statsFactory.Create(document, files.Count),
        };
    }
}
