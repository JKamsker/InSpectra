using System.Text.Json.Nodes;
using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Contracts;
using InSpectra.Gen.Engine.Execution.Process;
using InSpectra.Gen.Engine.UseCases.Generate;
using InSpectra.Gen.Engine.UseCases.Generate.Requests;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.OpenCli;

public sealed class OpenCliArtifactWriterTests
{
    [Fact]
    public async Task WriteArtifactsAsync_Rejects_Existing_Crawl_Output_Without_Overwrite()
    {
        using var temp = new TempDirectory();
        var crawlPath = Path.Combine(temp.Path, "crawl.json");
        File.WriteAllText(crawlPath, "{}");

        var exception = await Assert.ThrowsAsync<CliUsageException>(() =>
            OpenCliArtifactWriter.WriteArtifactsAsync(
                new OpenCliArtifactOptions(null, crawlPath, Overwrite: false),
                openCliJson: "{}",
                crawlJson: "{\"commands\":[]}",
                CancellationToken.None));

        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WriteArtifactsAsync_Writes_Artifacts_And_Returns_Resolved_Paths()
    {
        using var temp = new TempDirectory();
        var openCliPath = Path.Combine(temp.Path, "nested", "opencli.json");
        var crawlPath = Path.Combine(temp.Path, "nested", "crawl.json");

        var result = await OpenCliArtifactWriter.WriteArtifactsAsync(
            new OpenCliArtifactOptions(openCliPath, crawlPath, Overwrite: false),
            openCliJson: "{\"info\":{}}",
            crawlJson: "{\"commands\":[]}",
            CancellationToken.None);

        Assert.Equal(Path.GetFullPath(openCliPath), result.OpenCliOutputPath);
        Assert.Equal(Path.GetFullPath(crawlPath), result.CrawlOutputPath);
        Assert.Equal("{\"info\":{}}", await File.ReadAllTextAsync(openCliPath));
        Assert.Equal("{\"commands\":[]}", await File.ReadAllTextAsync(crawlPath));
    }

    [Fact]
    public async Task WriteArtifactsAsync_Honors_Cancellation_Before_Write()
    {
        using var temp = new TempDirectory();
        var openCliPath = Path.Combine(temp.Path, "opencli.json");
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            OpenCliArtifactWriter.WriteArtifactsAsync(
                new OpenCliArtifactOptions(openCliPath, null, Overwrite: false),
                openCliJson: "{\"info\":{}}",
                crawlJson: null,
                cancellationSource.Token));

        Assert.False(File.Exists(openCliPath));
    }

    [Fact]
    public async Task WriteArtifactsAsync_Preserves_Path_Validation_When_Cancelled()
    {
        using var temp = new TempDirectory();
        var crawlPath = Path.Combine(temp.Path, "crawl.json");
        File.WriteAllText(crawlPath, "{}");
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        var exception = await Assert.ThrowsAsync<CliUsageException>(() =>
            OpenCliArtifactWriter.WriteArtifactsAsync(
                new OpenCliArtifactOptions(null, crawlPath, Overwrite: false),
                openCliJson: "{}",
                crawlJson: "{\"commands\":[]}",
                cancellationSource.Token));

        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WriteArtifactsAsync_Validates_All_Requested_Paths_Before_Cancellation()
    {
        using var temp = new TempDirectory();
        var openCliPath = Path.Combine(temp.Path, "opencli.json");
        var crawlPath = Path.Combine(temp.Path, "crawl.json");
        File.WriteAllText(crawlPath, "{}");
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        var exception = await Assert.ThrowsAsync<CliUsageException>(() =>
            OpenCliArtifactWriter.WriteArtifactsAsync(
                new OpenCliArtifactOptions(openCliPath, crawlPath, Overwrite: false),
                openCliJson: "{\"info\":{}}",
                crawlJson: "{\"commands\":[]}",
                cancellationSource.Token));

        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(openCliPath));
    }

    [Fact]
    public async Task Native_acquisition_uses_reported_executable_path_in_result_source()
    {
        var support = new OpenCliNativeAcquisitionSupport(new FakeProcessRunner());

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
                null,
                30),
            warnings: [],
            cancellationToken: CancellationToken.None);

        Assert.Equal("C:\\tools\\demo.exe", result.Source.ExecutablePath);
    }

    [Fact]
    public async Task Native_try_acquire_writes_artifacts_and_records_paths_in_metadata()
    {
        using var temp = new TempDirectory();
        var openCliPath = Path.Combine(temp.Path, "opencli.json");
        var support = new OpenCliNativeAcquisitionSupport(new FakeProcessRunner());

        var result = await support.TryAcquireAsync(
            new AcquisitionResultContext(
                "exec",
                "demo",
                "C:\\tools\\demo.exe",
                "demo",
                null,
                new OpenCliArtifactOptions(openCliPath, null)),
            new NativeProcessOptions(
                "C:\\temp\\inspectra-local-target.cmd",
                [],
                [],
                false,
                [],
                Environment.CurrentDirectory,
                null,
                null,
                30),
            attempts: [],
            warnings: [],
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(openCliPath), result.Metadata.OpenCliOutputPath);
        Assert.Equal(
            JsonNode.Parse(result.OpenCliJson)?.ToJsonString(),
            JsonNode.Parse(await File.ReadAllTextAsync(openCliPath))?.ToJsonString());
    }

    [Fact]
    public async Task Native_acquisition_writes_artifacts_and_records_paths_in_metadata()
    {
        using var temp = new TempDirectory();
        var openCliPath = Path.Combine(temp.Path, "opencli.json");
        var support = new OpenCliNativeAcquisitionSupport(new FakeProcessRunner());

        var result = await support.AcquireAsync(
            new AcquisitionResultContext(
                "exec",
                "demo",
                "C:\\tools\\demo.exe",
                "demo",
                null,
                new OpenCliArtifactOptions(openCliPath, null)),
            new NativeProcessOptions(
                "C:\\temp\\inspectra-local-target.cmd",
                [],
                [],
                false,
                [],
                Environment.CurrentDirectory,
                null,
                null,
                30),
            warnings: [],
            cancellationToken: CancellationToken.None);

        Assert.Equal(Path.GetFullPath(openCliPath), result.Metadata.OpenCliOutputPath);
        Assert.Equal(
            JsonNode.Parse(result.OpenCliJson)?.ToJsonString(),
            JsonNode.Parse(await File.ReadAllTextAsync(openCliPath))?.ToJsonString());
    }

    [Fact]
    public async Task Native_try_acquire_preserves_exception_details_in_failed_attempt()
    {
        var attempts = new List<OpenCliAcquisitionAttempt>();
        var support = new OpenCliNativeAcquisitionSupport(new ThrowingProcessRunner(
            new CliSourceExecutionException(
                "Native analysis failed.",
                details:
                [
                    "Arguments: inspect --opencli",
                    "Standard output:\nusage details",
                    "Standard error:\nexecution failed",
                ])));

        var result = await support.TryAcquireAsync(
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
                null,
                30),
            attempts,
            warnings: [],
            cancellationToken: CancellationToken.None);

        Assert.Null(result);
        var attempt = Assert.Single(attempts);
        Assert.NotNull(attempt.Detail);
        Assert.Contains("Native analysis failed.", attempt.Detail, StringComparison.Ordinal);
        Assert.Contains("Arguments: inspect --opencli", attempt.Detail, StringComparison.Ordinal);
        Assert.Contains("Standard output:", attempt.Detail, StringComparison.Ordinal);
        Assert.Contains("Standard error:", attempt.Detail, StringComparison.Ordinal);
    }

}

internal sealed class ThrowingProcessRunner(CliException exception) : IProcessRunner
{
    public Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => Task.FromException<ProcessResult>(exception);

    public Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        IReadOnlyDictionary<string, string>? environment,
        string? cleanupRoot,
        CancellationToken cancellationToken)
        => Task.FromException<ProcessResult>(exception);
}
