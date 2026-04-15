namespace InSpectra.Lib.Tooling.Tools;


public sealed record ToolDescriptor(
    string PackageId,
    string Version,
    string? CommandName,
    string? CliFramework,
    string PreferredAnalysisMode,
    string SelectionReason,
    string PackageUrl,
    string? PackageContentUrl,
    string? CatalogEntryUrl,
    string? PackageTitle = null,
    string? PackageDescription = null,
    string? HookCliFramework = null);

