namespace InSpectra.Gen.Engine.Modes.CliFx.Metadata;

internal sealed record CliFxOptionDefinition(
    string? Name,
    char? ShortName,
    bool IsRequired,
    bool IsSequence,
    bool IsBoolLike,
    string? ClrType,
    string? Description,
    string? EnvironmentVariable,
    IReadOnlyList<string> AcceptedValues,
    string? ValueName = null);
