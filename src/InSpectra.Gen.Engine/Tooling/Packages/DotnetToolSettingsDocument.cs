namespace InSpectra.Gen.Engine.Tooling.Packages;


internal sealed record DotnetToolSettingsDocument(
    string ToolDirectory,
    IReadOnlyList<DotnetToolSettingsCommand> Commands);
