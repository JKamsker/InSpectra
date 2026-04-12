using InSpectra.Lib;
using InSpectra.Lib.Contracts;
using InSpectra.Lib.UseCases.Generate;
using InSpectra.Lib.UseCases.Generate.Requests;

namespace InSpectra.Gen.Tests.OpenCli;

public sealed class OpenCliArtifactWriterNativeInvalidJsonTests
{
    [Fact]
    public async Task Native_try_acquire_treats_invalid_json_output_as_failed_attempt()
    {
        var attempts = new List<OpenCliAcquisitionAttempt>();
        var support = new OpenCliNativeAcquisitionSupport(new InvalidJsonProcessRunner("invalid json"));

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
        Assert.Contains("invalid-success-artifact", attempt.Detail, StringComparison.Ordinal);
        Assert.Contains("Parser error:", attempt.Detail, StringComparison.Ordinal);
        Assert.Contains("Standard output:", attempt.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Native_acquire_surfaces_invalid_json_output_as_cli_exception()
    {
        var support = new OpenCliNativeAcquisitionSupport(new InvalidJsonProcessRunner("invalid json"));

        var exception = await Assert.ThrowsAsync<CliSourceExecutionException>(() =>
            support.AcquireAsync(
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
                cancellationToken: CancellationToken.None));

        Assert.Contains("invalid OpenCLI artifact", exception.Message, StringComparison.Ordinal);
        Assert.Contains(exception.Details ?? [], detail => detail.Contains("invalid-success-artifact", StringComparison.Ordinal));
    }
}
