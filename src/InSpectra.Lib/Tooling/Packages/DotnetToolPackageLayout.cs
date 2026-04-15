namespace InSpectra.Lib.Tooling.Packages;


public sealed record DotnetToolPackageLayout(
    IReadOnlyList<string> ToolSettingsPaths,
    IReadOnlyList<string> ToolCommandNames,
    IReadOnlyList<string> ToolEntryPointPaths,
    IReadOnlySet<string> ToolDirectories);
