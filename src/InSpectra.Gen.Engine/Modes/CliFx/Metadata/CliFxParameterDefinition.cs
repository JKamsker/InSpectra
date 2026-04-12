namespace InSpectra.Gen.Engine.Modes.CliFx.Metadata;

internal sealed record CliFxParameterDefinition(
    int Order,
    string Name,
    bool IsRequired,
    bool IsSequence,
    string? ClrType,
    string? Description,
    IReadOnlyList<string> AcceptedValues);
