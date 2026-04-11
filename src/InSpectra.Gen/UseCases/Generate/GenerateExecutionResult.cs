using InSpectra.Gen.UseCases.Generate.Requests;
using InSpectra.Gen.Rendering.Contracts;

namespace InSpectra.Gen.UseCases.Generate;

public sealed record GenerateExecutionResult(
    RenderSourceInfo Source,
    OpenCliAcquisitionMetadata Acquisition,
    IReadOnlyList<string> Warnings,
    string OpenCliJson,
    string? OutputFile);
