using InSpectra.Gen.UseCases.Generate.Requests;

namespace InSpectra.Gen.UseCases.Generate;

public interface IOpenCliAcquisitionService
{
    Task<OpenCliAcquisitionResult> AcquireFromExecAsync(
        ExecAcquisitionRequest request,
        CancellationToken cancellationToken);

    Task<OpenCliAcquisitionResult> AcquireFromPackageAsync(
        PackageAcquisitionRequest request,
        CancellationToken cancellationToken);

    Task<OpenCliAcquisitionResult> AcquireFromDotnetAsync(
        DotnetAcquisitionRequest request,
        CancellationToken cancellationToken);
}
