using InSpectra.Gen.Runtime;
using InSpectra.Gen.Acquisition.Runtime;
using InSpectra.Gen.Runtime.Acquisition;
using InSpectra.Gen.Runtime.Rendering;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests;

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
    public async Task Generate_from_exec_rejects_output_file_that_matches_crawl_output()
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "artifacts.json");
        var acquisitionService = new FakeAcquisitionService();
        var service = CreateService(acquisitionService);
        var request = CreateExecRequest(temp.Path) with
        {
            Artifacts = new OpenCliArtifactOptions(null, outputPath),
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
            Mode: OpenCliMode.Auto,
            CommandName: "demo",
            CliFramework: null,
            OpenCliArguments: ["cli", "opencli"],
            IncludeXmlDoc: false,
            XmlDocArguments: ["cli", "xmldoc"],
            WorkingDirectory: workingDirectory,
            TimeoutSeconds: 30,
            Artifacts: new OpenCliArtifactOptions(null, null));
    }

    private sealed class FakeAcquisitionService : IOpenCliAcquisitionService
    {
        private readonly string _openCliJson = File.ReadAllText(FixturePaths.OpenCliJson);

        public int ExecCalls { get; private set; }

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
                XmlDocument: null,
                CrawlJson: null,
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
