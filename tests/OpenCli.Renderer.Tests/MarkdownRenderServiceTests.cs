using OpenCli.Renderer.Runtime;
using OpenCli.Renderer.Services;
using OpenCli.Renderer.Tests.TestSupport;

namespace OpenCli.Renderer.Tests;

public class MarkdownRenderServiceTests
{
    private readonly MarkdownRenderService _service = new(
        RendererFactory.CreateDocumentRenderService(),
        new OpenCliNormalizer(),
        RendererFactory.CreateMarkdownRenderer(),
        new RenderStatsFactory());

    [Fact]
    public async Task Dry_run_does_not_write_single_output_file()
    {
        using var temp = new TempDirectory();
        var outputFile = Path.Combine(temp.Path, "docs.md");
        var request = new FileRenderRequest(
            FixturePaths.OpenCliJson,
            null,
            new RenderExecutionOptions(
                RenderLayout.Single,
                ResolvedOutputMode.Human,
                DryRun: true,
                Quiet: false,
                Verbose: false,
                NoColor: false,
                IncludeHidden: false,
                IncludeMetadata: false,
                Overwrite: false,
                OutputFile: outputFile,
                OutputDirectory: null));

        var result = await _service.RenderFromFileAsync(request, CancellationToken.None);

        Assert.False(File.Exists(outputFile));
        Assert.True(result.IsDryRun);
        Assert.Single(result.Files);
        Assert.Equal(DocumentFormat.Markdown, result.Format);
        Assert.Equal(RenderLayout.Single, result.Layout);
    }

    [Fact]
    public async Task Tree_render_refuses_non_empty_directory_without_overwrite()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "keep.txt"), "existing");

        var request = new FileRenderRequest(
            FixturePaths.OpenCliJson,
            null,
            new RenderExecutionOptions(
                RenderLayout.Tree,
                ResolvedOutputMode.Human,
                DryRun: false,
                Quiet: false,
                Verbose: false,
                NoColor: false,
                IncludeHidden: false,
                IncludeMetadata: false,
                Overwrite: false,
                OutputFile: null,
                OutputDirectory: temp.Path));

        var exception = await Assert.ThrowsAsync<CliUsageException>(() =>
            _service.RenderFromFileAsync(request, CancellationToken.None));

        Assert.Contains("not empty", exception.Message);
    }
}
