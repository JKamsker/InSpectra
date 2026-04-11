namespace InSpectra.Gen.UseCases.Generate.Requests;

public sealed record ExecAcquisitionRequest(
    string Source,
    IReadOnlyList<string> SourceArguments,
    string WorkingDirectory,
    AcquisitionOptions Options);
