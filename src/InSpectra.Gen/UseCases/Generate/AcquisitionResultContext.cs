using InSpectra.Gen.UseCases.Generate.Requests;

namespace InSpectra.Gen.UseCases.Generate;

internal sealed record AcquisitionResultContext(
    string Kind,
    string SourceLabel,
    string? ReportedExecutablePath,
    string? CommandName,
    string? CliFramework,
    OpenCliArtifactOptions Artifacts);
