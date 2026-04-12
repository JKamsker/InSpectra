namespace InSpectra.Gen.Engine.UseCases.Generate.Requests;

public sealed record DotnetAcquisitionRequest(
    DotnetBuildSettings Build,
    string WorkingDirectory,
    AcquisitionOptions Options);
