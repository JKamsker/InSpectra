using InSpectra.Gen.Runtime.Acquisition;

namespace InSpectra.Gen.OpenCli.Acquisition;

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
