using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Rendering.Contracts;
using InSpectra.Gen.Engine.Rendering.Pipeline;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.OpenCli;

public class OpenCliFileLoadingErrorTests
{
    private readonly OpenCliDocumentLoader _loader = new(new OpenCliSchemaProvider());
    private readonly OpenCliXmlEnricher _enricher = new();

    [Fact]
    public async Task Load_from_file_rejects_directory_paths_with_cli_usage_exception()
    {
        using var temp = new TempDirectory();
        var directoryPath = Path.Combine(temp.Path, "folder");
        Directory.CreateDirectory(directoryPath);

        var exception = await Assert.ThrowsAsync<CliUsageException>(() =>
            _loader.LoadFromFileAsync(directoryPath, CancellationToken.None));

        Assert.Equal($"OpenCLI file `{Path.GetFullPath(directoryPath)}` does not exist.", exception.Message);
    }

    [Fact]
    public async Task Load_from_file_preserves_missing_path_error_when_token_is_pre_canceled()
    {
        using var temp = new TempDirectory();
        var missingPath = Path.Combine(temp.Path, "missing.json");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exception = await Assert.ThrowsAsync<CliUsageException>(() =>
            _loader.LoadFromFileAsync(missingPath, cts.Token));

        Assert.Equal($"OpenCLI file `{Path.GetFullPath(missingPath)}` does not exist.", exception.Message);
    }

    [Fact]
    public async Task Xml_enrichment_rejects_directory_paths_with_cli_usage_exception()
    {
        using var temp = new TempDirectory();
        var directoryPath = Path.Combine(temp.Path, "folder");
        Directory.CreateDirectory(directoryPath);
        var document = await _loader.LoadFromFileAsync(FixturePaths.OpenCliJson, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<CliUsageException>(() =>
            _enricher.EnrichFromFileAsync(document, directoryPath, CancellationToken.None));

        Assert.Equal($"XML enrichment file `{Path.GetFullPath(directoryPath)}` does not exist.", exception.Message);
    }

    [Fact]
    public async Task Xml_enrichment_preserves_missing_path_error_when_token_is_pre_canceled()
    {
        using var temp = new TempDirectory();
        var missingPath = Path.Combine(temp.Path, "missing.xml");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var document = await _loader.LoadFromFileAsync(FixturePaths.OpenCliJson, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<CliUsageException>(() =>
            _enricher.EnrichFromFileAsync(document, missingPath, cts.Token));

        Assert.Equal($"XML enrichment file `{Path.GetFullPath(missingPath)}` does not exist.", exception.Message);
    }

    [Fact]
    public async Task Document_render_service_rejects_directory_xml_paths_with_cli_usage_exception()
    {
        using var temp = new TempDirectory();
        var directoryPath = Path.Combine(temp.Path, "folder");
        Directory.CreateDirectory(directoryPath);
        var service = RendererFactory.CreateDocumentRenderService();

        var exception = await Assert.ThrowsAsync<CliUsageException>(() =>
            service.LoadFromFileAsync(
                new FileRenderRequest(
                    FixturePaths.OpenCliJson,
                    directoryPath,
                    new RenderExecutionOptions(
                        RenderLayout.Single,
                        DryRun: true,
                        IncludeHidden: false,
                        IncludeMetadata: false,
                        Overwrite: false,
                        SingleFile: false,
                        CompressLevel: 0,
                        OutputFile: null,
                        OutputDirectory: null)),
                CancellationToken.None));

        Assert.Equal($"XML enrichment file `{Path.GetFullPath(directoryPath)}` does not exist.", exception.Message);
    }

    [Fact]
    public async Task Document_render_service_preserves_missing_xml_path_error_when_token_is_pre_canceled()
    {
        using var temp = new TempDirectory();
        var missingPath = Path.Combine(temp.Path, "missing.xml");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exception = await Assert.ThrowsAsync<CliUsageException>(() =>
            DocumentRenderService.LoadXmlDocumentAsync(missingPath, cts.Token));

        Assert.Equal($"XML enrichment file `{Path.GetFullPath(missingPath)}` does not exist.", exception.Message);
    }
}
