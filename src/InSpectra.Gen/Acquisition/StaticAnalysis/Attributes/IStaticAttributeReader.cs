namespace InSpectra.Gen.Acquisition.StaticAnalysis.Attributes;

using InSpectra.Gen.Acquisition.StaticAnalysis.Models;

using InSpectra.Gen.Acquisition.StaticAnalysis.Inspection;

internal interface IStaticAttributeReader
{
    IReadOnlyDictionary<string, StaticCommandDefinition> Read(IReadOnlyList<ScannedModule> modules);
}

