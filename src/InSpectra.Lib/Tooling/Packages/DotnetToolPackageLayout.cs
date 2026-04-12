namespace InSpectra.Lib.Tooling.Packages;


internal sealed record DotnetToolPackageLayout(
    IReadOnlyList<string> ToolSettingsPaths,
    IReadOnlyList<string> ToolCommandNames,
    IReadOnlyList<string> ToolEntryPointPaths,
    IReadOnlySet<string> ToolDirectories);
