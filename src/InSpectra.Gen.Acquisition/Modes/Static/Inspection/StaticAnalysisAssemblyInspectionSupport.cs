namespace InSpectra.Gen.Acquisition.Modes.Static.Inspection;

using InSpectra.Gen.Acquisition.Tooling.FrameworkDetection;

using InSpectra.Gen.Acquisition.Modes.Static.Attributes;
using InSpectra.Gen.Acquisition.Modes.Static.Models;


using dnlib.DotNet;

internal sealed class StaticAnalysisAssemblyInspectionSupport
{
    private readonly DnlibAssemblyScanner _assemblyScanner;

    public StaticAnalysisAssemblyInspectionSupport(DnlibAssemblyScanner assemblyScanner)
    {
        _assemblyScanner = assemblyScanner;
    }

    public StaticAnalysisAssemblyInspectionResult InspectAssemblies(
        string installDirectory,
        string cliFramework,
        string? preferredEntryPointPath)
    {
        var adapter = CliFrameworkProviderRegistry.ResolveStaticAnalysisAdapter(cliFramework);
        if (adapter is null)
        {
            return StaticAnalysisAssemblyInspectionResult.NoReader(cliFramework);
        }

        var modules = _assemblyScanner.ScanForFramework(
            installDirectory,
            adapter.AssemblyName,
            preferredEntryPointPath);
        if (modules.Count == 0)
        {
            return StaticAnalysisAssemblyInspectionResult.FrameworkNotFound(cliFramework);
        }

        try
        {
            var reader = (IStaticAttributeReader)adapter.Reader;
            var commands = new Dictionary<string, StaticCommandDefinition>(reader.Read(modules), StringComparer.OrdinalIgnoreCase);
            if (commands.Count == 0)
            {
                return StaticAnalysisAssemblyInspectionResult.NoAttributes(cliFramework, modules.Count);
            }

            return StaticAnalysisAssemblyInspectionResult.Ok(cliFramework, modules.Count, commands);
        }
        finally
        {
            foreach (var module in modules)
            {
                module.Dispose();
            }
        }
    }
}

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
