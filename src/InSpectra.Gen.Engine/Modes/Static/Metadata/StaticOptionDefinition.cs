namespace InSpectra.Gen.Engine.Modes.Static.Metadata;

internal sealed record StaticOptionDefinition(
    string? LongName,
    char? ShortName,
    bool IsRequired,
    bool IsSequence,
    bool IsBoolLike,
    string? ClrType,
    string? Description,
    string? DefaultValue,
    string? MetaValue,
    IReadOnlyList<string> AcceptedValues,
    string? PropertyName = null);
