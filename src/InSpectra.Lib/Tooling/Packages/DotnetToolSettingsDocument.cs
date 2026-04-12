namespace InSpectra.Lib.Tooling.Packages;


internal sealed record DotnetToolSettingsDocument(
    string ToolDirectory,
    IReadOnlyList<DotnetToolSettingsCommand> Commands);
