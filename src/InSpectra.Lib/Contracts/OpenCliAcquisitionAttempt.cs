namespace InSpectra.Lib.Contracts;

public sealed record OpenCliAcquisitionAttempt(
    string Mode,
    string? Framework,
    string Outcome,
    string? Detail = null);
