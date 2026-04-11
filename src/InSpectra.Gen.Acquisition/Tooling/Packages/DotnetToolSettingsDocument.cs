namespace InSpectra.Gen.Acquisition.Tooling.Packages;


internal sealed record DotnetToolSettingsDocument(
    string ToolDirectory,
    IReadOnlyList<DotnetToolSettingsCommand> Commands);
