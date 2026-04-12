using InSpectra.Lib.Contracts;
using InSpectra.Lib.Rendering.Contracts;
using InSpectra.Lib.UseCases.Generate.Requests;

namespace InSpectra.Lib.UseCases.Generate;

public sealed record GenerateExecutionResult(
    RenderSourceInfo Source,
    OpenCliAcquisitionMetadata Acquisition,
    IReadOnlyList<string> Warnings,
    string OpenCliJson,
    string? OutputFile);
