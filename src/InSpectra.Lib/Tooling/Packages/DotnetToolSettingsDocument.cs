namespace InSpectra.Lib.Tooling.Packages;


public sealed record DotnetToolSettingsDocument(
    string ToolDirectory,
    IReadOnlyList<DotnetToolSettingsCommand> Commands);
