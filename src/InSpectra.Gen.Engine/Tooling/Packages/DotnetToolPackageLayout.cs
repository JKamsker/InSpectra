namespace InSpectra.Gen.Engine.Tooling.Packages;


internal sealed record DotnetToolPackageLayout(
    IReadOnlyList<string> ToolSettingsPaths,
    IReadOnlyList<string> ToolCommandNames,
    IReadOnlyList<string> ToolEntryPointPaths,
    IReadOnlySet<string> ToolDirectories);
