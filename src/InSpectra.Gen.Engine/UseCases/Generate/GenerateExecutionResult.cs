using InSpectra.Gen.Engine.Contracts;
using InSpectra.Gen.Engine.Rendering.Contracts;
using InSpectra.Gen.Engine.UseCases.Generate.Requests;

namespace InSpectra.Gen.Engine.UseCases.Generate;

public sealed record GenerateExecutionResult(
    RenderSourceInfo Source,
    OpenCliAcquisitionMetadata Acquisition,
    IReadOnlyList<string> Warnings,
    string OpenCliJson,
    string? OutputFile);
