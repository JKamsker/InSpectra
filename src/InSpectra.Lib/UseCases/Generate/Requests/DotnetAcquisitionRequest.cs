namespace InSpectra.Lib.UseCases.Generate.Requests;

public sealed record DotnetAcquisitionRequest(
    DotnetBuildSettings Build,
    string WorkingDirectory,
    AcquisitionOptions Options);
