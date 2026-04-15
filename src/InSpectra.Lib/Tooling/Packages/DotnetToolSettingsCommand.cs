namespace InSpectra.Lib.Tooling.Packages;


public sealed record DotnetToolSettingsCommand(
    string? CommandName,
    string? EntryPointPath);
