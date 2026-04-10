using InSpectra.Gen.Runtime;

namespace InSpectra.Gen.Services;

public interface IOpenCliGenerationService
{
    Task<GenerateExecutionResult> GenerateFromExecAsync(
        ExecAcquisitionRequest request,
        string? outputFile,
        CancellationToken cancellationToken);

    Task<GenerateExecutionResult> GenerateFromDotnetAsync(
        DotnetAcquisitionRequest request,
        string? outputFile,
        CancellationToken cancellationToken);

    Task<GenerateExecutionResult> GenerateFromPackageAsync(
        PackageAcquisitionRequest request,
        string? outputFile,
        CancellationToken cancellationToken);
}
