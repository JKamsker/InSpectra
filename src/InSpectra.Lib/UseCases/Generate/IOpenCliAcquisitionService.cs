using InSpectra.Lib.UseCases.Generate.Requests;

namespace InSpectra.Lib.UseCases.Generate;

internal interface IOpenCliAcquisitionService
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
