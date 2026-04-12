namespace InSpectra.Gen.Acquisition.Modes.Static.Inspection;

using InSpectra.Gen.Acquisition.Modes.Static.Attributes;
using InSpectra.Gen.Acquisition.Modes.Static.Metadata;
using InSpectra.Gen.Acquisition.Tooling.FrameworkDetection;

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
            // Runtime cast is intentional. The adapter carries the reader as `object`
            // on purpose so that Tooling/FrameworkDetection has no compile-time
            // dependency on Modes.Static.Attributes (a Tooling -> Modes dependency is
            // forbidden by the architecture charter). Promoting IStaticAttributeReader
            // into Contracts/ would not help either, because its signature references
            // Static-mode-owned types (StaticCommandDefinition and dnlib-backed
            // ScannedModule), which would turn the erasure into an even worse
            // Contracts -> Modes leak. See StaticAnalysisFrameworkAdapter and
            // IStaticAttributeReader for the full rationale.
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
