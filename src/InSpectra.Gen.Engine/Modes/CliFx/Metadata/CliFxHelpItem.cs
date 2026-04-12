namespace InSpectra.Gen.Engine.Modes.CliFx.Metadata;

internal sealed record CliFxHelpItem(
    string Key,
    bool IsRequired,
    string? Description);
