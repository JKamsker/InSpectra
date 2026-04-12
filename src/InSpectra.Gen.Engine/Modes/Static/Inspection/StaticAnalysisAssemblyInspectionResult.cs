namespace InSpectra.Gen.Engine.Modes.Static.Inspection;

using InSpectra.Gen.Engine.Modes.Static.Metadata;

internal sealed record StaticAnalysisAssemblyInspectionResult(
    string InspectionOutcome,
    string? ClaimedFramework,
    int ScannedModuleCount,
    Dictionary<string, StaticCommandDefinition> Commands)
{
    public static StaticAnalysisAssemblyInspectionResult Ok(string framework, int moduleCount, Dictionary<string, StaticCommandDefinition> commands)
        => new("ok", framework, moduleCount, commands);

    public static StaticAnalysisAssemblyInspectionResult FrameworkNotFound(string claimedFramework)
        => new("framework-not-found", claimedFramework, 0, new(StringComparer.OrdinalIgnoreCase));

    public static StaticAnalysisAssemblyInspectionResult NoAttributes(string framework, int moduleCount)
        => new("no-attributes", framework, moduleCount, new(StringComparer.OrdinalIgnoreCase));

    public static StaticAnalysisAssemblyInspectionResult NoReader(string framework)
        => new("no-reader", framework, 0, new(StringComparer.OrdinalIgnoreCase));
}
