namespace InSpectra.Gen.Acquisition.Tests;

using InSpectra.Discovery.Tool.Help.Documents;
using InSpectra.Discovery.Tool.StaticAnalysis.Models;
using InSpectra.Discovery.Tool.StaticAnalysis.OpenCli;

public sealed class StaticAnalysisOpenCliBuilderOptionTests
{
    [Fact]
    public void Build_Skips_Metadata_Only_Builtin_Help_And_Version_Flags()
    {
        var builder = new StaticAnalysisOpenCliBuilder();
        var staticCommands = new Dictionary<string, StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = new(
                Name: null,
                Description: "Default command",
                IsDefault: true,
                IsHidden: false,
                Values: [],
                Options:
                [
                    new StaticOptionDefinition("help", 'h', false, false, true, "System.Boolean", "Show help information.", null, null, [], "Help"),
                    new StaticOptionDefinition("version", null, false, false, true, "System.Boolean", "Display version information.", null, null, [], "Version"),
                    new StaticOptionDefinition("config", 'c', true, false, false, "System.String", "Configuration path.", null, "CONFIG", [], "Config"),
                ]),
        };

        var document = builder.Build(
            "demo",
            "1.0.0",
            "System.CommandLine",
            staticCommands,
            new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase));

        var config = Assert.Single(document["options"]!.AsArray());
        Assert.Equal("--config", config!["name"]?.GetValue<string>());
    }

    [Fact]
    public void Build_Maps_Slash_Style_Help_Options_Into_Publishable_Names()
    {
        var builder = new StaticAnalysisOpenCliBuilder();
        var staticCommands = new Dictionary<string, StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["root"] = new("root", null, false, false, [], []),
        };
        var helpDocuments = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(
                title: "MonoGame Content Builder:",
                version: "v3.8.5.0",
                options:
                [
                    new Item("/b, /build:<sourceFile>", false, "Build the content source file."),
                    new Item("/c, /clean", false, "Delete all previously built content and intermediate files."),
                    new Item("/compress", false, "Compress the XNB files for smaller file sizes."),
                    new Item("/h, /help", false, "Displays this help."),
                ]),
        };

        var document = builder.Build("mgcb", "3.8.5-preview.3", "System.CommandLine", staticCommands, helpDocuments);

        var options = document["options"]!.AsArray()
            .OfType<System.Text.Json.Nodes.JsonObject>()
            .Select(option => option["name"]!.GetValue<string>())
            .ToArray();
        Assert.Equal(["--build", "--clean", "--compress", "--help"], options);
    }
}
