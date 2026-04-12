namespace InSpectra.Lib.Modes.Static.Metadata;

internal sealed record StaticCommandDefinition(
    string? Name,
    string? Description,
    bool IsDefault,
    bool IsHidden,
    IReadOnlyList<StaticValueDefinition> Values,
    IReadOnlyList<StaticOptionDefinition> Options);
