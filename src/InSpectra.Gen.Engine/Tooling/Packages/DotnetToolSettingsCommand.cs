namespace InSpectra.Gen.Engine.Tooling.Packages;


internal sealed record DotnetToolSettingsCommand(
    string? CommandName,
    string? EntryPointPath);
