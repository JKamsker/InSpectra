using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Rendering.Contracts;
using InSpectra.Gen.Engine.Rendering.Pipeline;
using InSpectra.Gen.Engine.Rendering.Pipeline.Model;

namespace InSpectra.Gen.Engine.Rendering.Markdown;

internal sealed class MarkdownRenderService(
    IDocumentRenderService documentService,
    OpenCliNormalizer normalizer,
    MarkdownRenderer renderer,
    RenderStatsFactory statsFactory)
    : IMarkdownRenderService
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
            ?? throw new CliUsageException("Hybrid Markdown layout requires an output directory.");
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
                planned);
        }

        var writtenFiles = await OutputPathHelper.PublishDirectoryAsync(
            outputDirectory,
            options.Overwrite,
            (stagingDirectory, token) => WriteRenderedFilesAsync(stagingDirectory, outputDirectory, files, token),
            cancellationToken);
        return CreateResult(prepared, document, options, writtenFiles);
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
                plannedFiles);
        }

        if (options.OutputFile is null)
        {
            return CreateResult(prepared, document, options, [], stdoutDocument: content);
        }

        await OutputPathHelper.PublishFileAsync(options.OutputFile, content, options.Overwrite, cancellationToken);

        var written = new RenderedFile(Path.GetFileName(options.OutputFile), options.OutputFile, content);
        return CreateResult(prepared, document, options, [written]);
    }

    private async Task<RenderExecutionResult> HandleTreeLayoutAsync(
        AcquiredRenderDocument prepared,
        NormalizedCliDocument document,
        RenderExecutionOptions options,
        MarkdownRenderOptions? markdownOptions,
        CancellationToken cancellationToken)
    {
        var outputDirectory = options.OutputDirectory
            ?? throw new CliUsageException("Tree Markdown layout requires an output directory.");
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
                planned);
        }

        var writtenFiles = await OutputPathHelper.PublishDirectoryAsync(
            outputDirectory,
            options.Overwrite,
            (stagingDirectory, token) => WriteRenderedFilesAsync(stagingDirectory, outputDirectory, files, token),
            cancellationToken);
        return CreateResult(prepared, document, options, writtenFiles);
    }

    private static async Task<List<RenderedFile>> WriteRenderedFilesAsync(
        string stagingDirectory,
        string outputDirectory,
        IReadOnlyList<RelativeRenderedFile> files,
        CancellationToken cancellationToken)
    {
        var writtenFiles = new List<RenderedFile>();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stagingPath = Path.Combine(stagingDirectory, file.RelativePath);
            var directory = Path.GetDirectoryName(stagingPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(stagingPath, file.Content, cancellationToken);
            writtenFiles.Add(new RenderedFile(file.RelativePath, Path.Combine(outputDirectory, file.RelativePath), file.Content));
        }

        return writtenFiles;
    }

    private RenderExecutionResult CreateResult(
        AcquiredRenderDocument prepared,
        NormalizedCliDocument document,
        RenderExecutionOptions options,
        IReadOnlyList<RenderedFile> files,
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
            Stats = statsFactory.Create(document, files.Count),
        };
    }
}
