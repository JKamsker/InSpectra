using InSpectra.Gen.Core;
using System.Text.Json.Nodes;
using InSpectra.Gen.UseCases.Generate.Requests;
using InSpectra.Gen.Rendering.Contracts;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.OpenCli;

public class OpenCliGenerationServiceTests
{
    [Fact]
    public async Task Generate_from_exec_rejects_existing_output_without_overwrite()
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "opencli.json");
        await File.WriteAllTextAsync(outputPath, "{}");

        var service = CreateService();
        var request = CreateExecRequest(temp.Path);

        await Assert.ThrowsAsync<CliUsageException>(() =>
            service.GenerateFromExecAsync(request, outputPath, overwrite: false, CancellationToken.None));
    }

    [Fact]
    public async Task Generate_from_exec_overwrites_existing_output_when_requested()
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "opencli.json");
        await File.WriteAllTextAsync(outputPath, "{}");

        var service = CreateService();
        var request = CreateExecRequest(temp.Path);

        var result = await service.GenerateFromExecAsync(request, outputPath, overwrite: true, CancellationToken.None);
        var writtenJson = await File.ReadAllTextAsync(outputPath);

        Assert.Equal(outputPath, result.OutputFile);
        Assert.Equal(result.OpenCliJson, writtenJson);
        Assert.Contains("\"title\":", writtenJson, StringComparison.Ordinal);
        Assert.DoesNotContain("{}", writtenJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_from_exec_publishes_output_and_artifacts_in_one_transaction()
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "generated.json");
        var crawlPath = Path.Combine(temp.Path, "crawl.json");
        var service = CreateService(new FakeAcquisitionService(xmlDocument: File.ReadAllText(FixturePaths.XmlDoc), crawlJson: "{\"commands\":[]}"));
        var request = CreateExecRequest(temp.Path) with
        {
            Options = CreateExecRequest(temp.Path).Options with
            {
                Artifacts = new OpenCliArtifactOptions(outputPath, crawlPath, Overwrite: false),
            },
        };

        var result = await service.GenerateFromExecAsync(request, outputPath, overwrite: false, CancellationToken.None);
        var outputJson = await File.ReadAllTextAsync(outputPath);
        var crawlJson = await File.ReadAllTextAsync(crawlPath);

        Assert.Equal(Path.GetFullPath(outputPath), result.OutputFile);
        Assert.Equal(Path.GetFullPath(outputPath), result.Acquisition.OpenCliOutputPath);
        Assert.Equal(Path.GetFullPath(crawlPath), result.Acquisition.CrawlOutputPath);
        Assert.Equal(JsonNode.Parse(result.OpenCliJson)?.ToJsonString(), JsonNode.Parse(outputJson)?.ToJsonString());
        Assert.Equal("{\"commands\":[]}", crawlJson);
        Assert.NotEqual(
            JsonNode.Parse(FakeAcquisitionService.DefaultOpenCliJson)?.ToJsonString(),
            JsonNode.Parse(outputJson)?.ToJsonString());
    }

    [Fact]
    public async Task Generate_from_exec_does_not_publish_crawl_when_output_validation_fails()
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "generated.json");
        var crawlPath = Path.Combine(temp.Path, "crawl.json");
        await File.WriteAllTextAsync(outputPath, "{}");
        var acquisitionService = new FakeAcquisitionService(crawlJson: "{\"commands\":[]}");
        var service = CreateService(acquisitionService);
        var request = CreateExecRequest(temp.Path) with
        {
            Options = CreateExecRequest(temp.Path).Options with
            {
                Artifacts = new OpenCliArtifactOptions(null, crawlPath, Overwrite: false),
            },
        };

        await Assert.ThrowsAsync<CliUsageException>(() =>
            service.GenerateFromExecAsync(request, outputPath, overwrite: false, CancellationToken.None));

        Assert.Equal(1, acquisitionService.ExecCalls);
        Assert.False(File.Exists(crawlPath));
        Assert.Equal("{}", await File.ReadAllTextAsync(outputPath));
    }

    [Fact]
    public async Task Generate_from_exec_rolls_back_all_publications_when_cancelled()
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "generated.json");
        var crawlPath = Path.Combine(temp.Path, "crawl.json");
        var service = CreateService(new FakeAcquisitionService(crawlJson: "{\"commands\":[]}"));
        var request = CreateExecRequest(temp.Path) with
        {
            Options = CreateExecRequest(temp.Path).Options with
            {
                Artifacts = new OpenCliArtifactOptions(null, crawlPath, Overwrite: false),
            },
        };
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GenerateFromExecAsync(request, outputPath, overwrite: false, cancellationSource.Token));

        Assert.False(File.Exists(outputPath));
        Assert.False(File.Exists(crawlPath));
    }

    [Fact]
    public async Task Generate_from_exec_rejects_output_file_that_matches_crawl_output()
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "artifacts.json");
        var acquisitionService = new FakeAcquisitionService();
        var service = CreateService(acquisitionService);
        var request = CreateExecRequest(temp.Path) with
        {
            Options = CreateExecRequest(temp.Path).Options with
            {
                Artifacts = new OpenCliArtifactOptions(null, outputPath),
            },
        };

        var exception = await Assert.ThrowsAsync<CliUsageException>(() =>
            service.GenerateFromExecAsync(request, outputPath, overwrite: true, CancellationToken.None));

        Assert.Equal("`--out` and `--crawl-out` must point to different files.", exception.Message);
        Assert.Equal(0, acquisitionService.ExecCalls);
    }

    private static OpenCliGenerationService CreateService(FakeAcquisitionService? acquisitionService = null)
    {
        return new OpenCliGenerationService(
            acquisitionService ?? new FakeAcquisitionService(),
            new OpenCliDocumentLoader(new OpenCliSchemaProvider()),
            new OpenCliXmlEnricher(),
            new OpenCliDocumentSerializer());
    }

    private static ExecAcquisitionRequest CreateExecRequest(string workingDirectory)
    {
        return new ExecAcquisitionRequest(
            Source: "demo",
            SourceArguments: [],
            WorkingDirectory: workingDirectory,
            Options: new AcquisitionOptions(
                OpenCliMode.Auto,
                "demo",
                null,
                ["cli", "opencli"],
                false,
                ["cli", "xmldoc"],
                30,
                new OpenCliArtifactOptions(null, null)));
    }

    private sealed class FakeAcquisitionService : IOpenCliAcquisitionService
    {
        public static string DefaultOpenCliJson { get; } = File.ReadAllText(FixturePaths.OpenCliJson);

        private readonly string _openCliJson;
        private readonly string? _xmlDocument;
        private readonly string? _crawlJson;

        public int ExecCalls { get; private set; }

        public FakeAcquisitionService(
            string? openCliJson = null,
            string? xmlDocument = null,
            string? crawlJson = null)
        {
            _openCliJson = openCliJson ?? DefaultOpenCliJson;
            _xmlDocument = xmlDocument;
            _crawlJson = crawlJson;
        }

        public Task<OpenCliAcquisitionResult> AcquireFromExecAsync(ExecAcquisitionRequest request, CancellationToken cancellationToken)
        {
            ExecCalls++;
            return Task.FromResult(CreateResult());
        }

        public Task<OpenCliAcquisitionResult> AcquireFromDotnetAsync(DotnetAcquisitionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(CreateResult());

        public Task<OpenCliAcquisitionResult> AcquireFromPackageAsync(PackageAcquisitionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(CreateResult());

        private OpenCliAcquisitionResult CreateResult()
        {
            return new OpenCliAcquisitionResult(
                _openCliJson,
                XmlDocument: _xmlDocument,
                CrawlJson: _crawlJson,
                new RenderSourceInfo("exec", "fake", null, "demo"),
                new OpenCliAcquisitionMetadata(
                    "auto",
                    "demo",
                    null,
                    [new OpenCliAcquisitionAttempt("auto", null, "success")],
                    null,
                    null),
                Warnings: []);
        }
    }
}
