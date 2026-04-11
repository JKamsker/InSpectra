using InSpectra.Gen.UseCases.Generate.Requests;

namespace InSpectra.Gen.UseCases.Generate;

public interface IOpenCliGenerationService
{
    Task<GenerateExecutionResult> GenerateFromExecAsync(
        ExecAcquisitionRequest request,
        string? outputFile,
        bool overwrite,
        CancellationToken cancellationToken);

    Task<GenerateExecutionResult> GenerateFromDotnetAsync(
        DotnetAcquisitionRequest request,
        string? outputFile,
        bool overwrite,
        CancellationToken cancellationToken);

    Task<GenerateExecutionResult> GenerateFromPackageAsync(
        PackageAcquisitionRequest request,
        string? outputFile,
        bool overwrite,
        CancellationToken cancellationToken);
}
