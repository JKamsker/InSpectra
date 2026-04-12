using InSpectra.Gen.Engine.UseCases.Generate.Requests;

namespace InSpectra.Gen.Engine.UseCases.Generate;

internal sealed record AcquisitionResultContext(
    string Kind,
    string SourceLabel,
    string? ReportedExecutablePath,
    string? CommandName,
    string? CliFramework,
    OpenCliArtifactOptions Artifacts);
