namespace InSpectra.Lib.Modes.CliFx.Metadata;

internal sealed record CliFxHelpItem(
    string Key,
    bool IsRequired,
    string? Description);
