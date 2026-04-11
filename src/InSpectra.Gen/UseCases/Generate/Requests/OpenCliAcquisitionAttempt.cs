namespace InSpectra.Gen.UseCases.Generate.Requests;

public sealed record OpenCliAcquisitionAttempt(
    string Mode,
    string? Framework,
    string Outcome,
    string? Detail = null);
