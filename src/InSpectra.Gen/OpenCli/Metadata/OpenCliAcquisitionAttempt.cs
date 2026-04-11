namespace InSpectra.Gen.OpenCli.Metadata;

public sealed record OpenCliAcquisitionAttempt(
    string Mode,
    string? Framework,
    string Outcome,
    string? Detail = null);
