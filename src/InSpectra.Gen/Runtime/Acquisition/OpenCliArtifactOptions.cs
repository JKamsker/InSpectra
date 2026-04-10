namespace InSpectra.Gen.Runtime.Acquisition;

public sealed record OpenCliArtifactOptions(
    string? OpenCliOutputPath,
    string? CrawlOutputPath,
    bool Overwrite = false);
