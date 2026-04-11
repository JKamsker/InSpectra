namespace InSpectra.Gen.Acquisition.Tests.OpenCli;

using InSpectra.Gen.Acquisition.Modes.CliFx.Metadata;
using InSpectra.Gen.Acquisition.Modes.CliFx.Projection;
using InSpectra.Gen.Acquisition.Contracts.Documents;
using InSpectra.Gen.Acquisition.Modes.Help.Projection;
using InSpectra.Gen.Acquisition.Contracts;
using InSpectra.Gen.Acquisition.Tests.StaticAnalysis;
using System.Text.Json.Nodes;

public sealed class OpenCliMetadataContractTests
{
    [Fact]
    public void Help_builder_uses_the_current_generator_name()
    {
        var builder = new OpenCliBuilder();
        var document = builder.Build(
            "demo",
            "1.0.0",
            new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase)
            {
                [""] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(
                    title: "demo",
                    version: "1.0.0",
                    description: "Demo application."),
            });

        Assert.Equal(InspectraProductInfo.GeneratorName, document["x-inspectra"]?["generator"]?.GetValue<string>());
    }

    [Fact]
    public void Clifx_builder_uses_the_current_generator_name()
    {
        var builder = new CliFxOpenCliBuilder();
        var document = builder.Build(
            "demo",
            "1.0.0",
            new Dictionary<string, CliFxCommandDefinition>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, CliFxHelpDocument>(StringComparer.OrdinalIgnoreCase)
            {
                [""] = new CliFxHelpDocument(
                    Title: "demo",
                    Version: "1.0.0",
                    ApplicationDescription: "Demo application.",
                    CommandDescription: null,
                    UsageLines: [],
                    Parameters: [],
                    Options: [],
                    Commands: []),
            });

        Assert.Equal(InspectraProductInfo.GeneratorName, document["x-inspectra"]?["generator"]?.GetValue<string>());
    }

    [Fact]
    public void Static_analysis_builder_uses_the_current_generator_name()
    {
        var builder = new InSpectra.Gen.Acquisition.Modes.Static.Projection.StaticAnalysisOpenCliBuilder();
        var document = builder.Build(
            "demo",
            "1.0.0",
            "System.CommandLine",
            new Dictionary<string, InSpectra.Gen.Acquisition.Modes.Static.Models.StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase));

        Assert.Equal(InspectraProductInfo.GeneratorName, document["x-inspectra"]?["generator"]?.GetValue<string>());
    }
}
