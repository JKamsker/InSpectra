namespace InSpectra.Gen.Engine.UseCases.Generate.Requests;

public sealed record PackageAcquisitionRequest(
    string PackageId,
    string Version,
    AcquisitionOptions Options);
