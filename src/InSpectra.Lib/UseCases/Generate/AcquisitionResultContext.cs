using InSpectra.Lib.UseCases.Generate.Requests;

namespace InSpectra.Lib.UseCases.Generate;

internal sealed record AcquisitionResultContext(
    string Kind,
    string SourceLabel,
    string? ReportedExecutablePath,
    string? CommandName,
    string? CliFramework,
    OpenCliArtifactOptions Artifacts);
