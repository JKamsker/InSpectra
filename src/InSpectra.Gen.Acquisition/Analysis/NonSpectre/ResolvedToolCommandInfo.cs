namespace InSpectra.Gen.Acquisition.Analysis.NonSpectre;


internal sealed record ResolvedToolCommandInfo(
    string? CommandName,
    string? EntryPointPath,
    string? ToolSettingsPath);
