using InSpectra.Gen.Acquisition.Contracts.Exceptions;
using InSpectra.Gen.Execution;
using InSpectra.Gen.UseCases.Generate;
using InSpectra.Gen.UseCases.Generate.Requests;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.OpenCli;

public sealed class OpenCliArtifactWriterTests
{
    [Fact]
    public void WriteArtifacts_Rejects_Existing_Crawl_Output_Without_Overwrite()
    {
        using var temp = new TempDirectory();
        var crawlPath = Path.Combine(temp.Path, "crawl.json");
        File.WriteAllText(crawlPath, "{}");

        var exception = Assert.Throws<CliUsageException>(() =>
            OpenCliArtifactWriter.WriteArtifacts(
                new OpenCliArtifactOptions(null, crawlPath, Overwrite: false),
                openCliJson: "{}",
                crawlJson: "{\"commands\":[]}"));

        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Native_acquisition_uses_reported_executable_path_in_result_source()
    {
        var processRunner = new FakeProcessRunner();
        var support = new OpenCliNativeAcquisitionSupport(processRunner);

        var result = await support.AcquireAsync(
            new AcquisitionResultContext(
                "exec",
                "demo",
                "C:\\tools\\demo.exe",
                "demo",
                null,
                new OpenCliArtifactOptions(null, null)),
            new NativeProcessOptions(
                "C:\\temp\\inspectra-local-target.cmd",
                [],
                [],
                false,
                [],
                Environment.CurrentDirectory,
                null,
                30),
            warnings: [],
            cancellationToken: CancellationToken.None);

        Assert.Equal("C:\\tools\\demo.exe", result.Source.ExecutablePath);
    }

    private sealed class FakeProcessRunner : IProcessRunner
    {
        public Task<ProcessResult> RunAsync(
            string executablePath,
            string workingDirectory,
            IReadOnlyList<string> arguments,
            int timeoutSeconds,
            CancellationToken cancellationToken)
            => RunAsync(executablePath, workingDirectory, arguments, timeoutSeconds, environment: null, cancellationToken);

        public Task<ProcessResult> RunAsync(
            string executablePath,
            string workingDirectory,
            IReadOnlyList<string> arguments,
            int timeoutSeconds,
            IReadOnlyDictionary<string, string>? environment,
            CancellationToken cancellationToken)
            => Task.FromResult(new ProcessResult(
                StandardOutput:
                """
                {
                  "openCliVersion": "0.1-draft",
                  "info": {
                    "title": "demo",
                    "version": "1.0.0"
                  }
                }
                """,
                StandardError: string.Empty));
    }
}
