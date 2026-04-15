using InSpectra.Lib.Rendering.Contracts;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.Rendering;

internal static class MarkdownRenderServiceTestSupport
{
    public static MarkdownRenderService CreateService()
    {
        return new MarkdownRenderService(
            RendererFactory.CreateDocumentRenderService(),
            new OpenCliNormalizer(),
            RendererFactory.CreateMarkdownRenderer(),
            new RenderStatsFactory());
    }

    public static async Task<RenderExecutionResult> RenderHybridAsync(
        MarkdownRenderService service,
        string outputDirectory,
        int splitDepth)
    {
        var request = new FileRenderRequest(
            FixturePaths.OpenCliJson,
            null,
            new RenderExecutionOptions(
                RenderLayout.Hybrid,
                DryRun: false,
                IncludeHidden: false,
                IncludeMetadata: false,
                Overwrite: true,
                SingleFile: false,
                CompressLevel: 0,
                OutputFile: null,
                OutputDirectory: outputDirectory),
            new MarkdownRenderOptions(HybridSplitDepth: splitDepth));

        return await service.RenderFromFileAsync(request, CancellationToken.None);
    }

    public static string ReadFile(RenderExecutionResult result, string relativePath)
    {
        var file = result.Files.Single(entry =>
            string.Equals(entry.RelativePath, relativePath, StringComparison.Ordinal));
        return file.Content ?? File.ReadAllText(file.FullPath);
    }
}
