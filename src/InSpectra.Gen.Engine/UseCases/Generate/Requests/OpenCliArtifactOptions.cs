namespace InSpectra.Gen.Engine.UseCases.Generate.Requests;

public sealed record OpenCliArtifactOptions(
    string? OpenCliOutputPath,
    string? CrawlOutputPath,
    bool Overwrite = false);
