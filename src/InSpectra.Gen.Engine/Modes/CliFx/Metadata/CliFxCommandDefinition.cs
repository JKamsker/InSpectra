namespace InSpectra.Gen.Engine.Modes.CliFx.Metadata;

internal sealed record CliFxCommandDefinition(
    string? Name,
    string? Description,
    IReadOnlyList<CliFxParameterDefinition> Parameters,
    IReadOnlyList<CliFxOptionDefinition> Options);
