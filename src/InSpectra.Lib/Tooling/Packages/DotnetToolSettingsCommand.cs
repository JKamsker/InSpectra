namespace InSpectra.Lib.Tooling.Packages;


internal sealed record DotnetToolSettingsCommand(
    string? CommandName,
    string? EntryPointPath);
