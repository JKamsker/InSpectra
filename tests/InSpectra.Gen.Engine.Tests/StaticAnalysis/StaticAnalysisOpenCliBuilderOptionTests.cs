namespace InSpectra.Gen.Engine.Tests.StaticAnalysis;

using InSpectra.Gen.Engine.Contracts.Documents;
using InSpectra.Gen.Engine.Modes.Static.Metadata;
using InSpectra.Gen.Engine.Modes.Static.Projection;

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

        var options = document["options"]!.AsArray().OfType<System.Text.Json.Nodes.JsonObject>().ToArray();
        Assert.Equal(["--build", "--clean", "--compress", "--help"], options.Select(option => option["name"]!.GetValue<string>()).ToArray());
        var build = Assert.Single(options, option => option["name"]!.GetValue<string>() == "--build");
        var argument = Assert.Single(build["arguments"]!.AsArray());
        Assert.Equal("SOURCEFILE", argument!["name"]!.GetValue<string>());
        Assert.True(argument["required"]!.GetValue<bool>());
        Assert.Equal(1, argument["arity"]!["minimum"]!.GetValue<int>());
    }

    [Fact]
    public void Build_Uses_Value_Requiredness_For_Optional_Options_With_Static_Metadata()
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
                    new StaticOptionDefinition("config", 'c', false, false, false, "System.String", "Configuration path.", null, "PATH", [], "Config"),
                ]),
        };
        var helpDocuments = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(
                options: [new Item("--config", false, null)]),
        };

        var document = builder.Build("demo", "1.0.0", "System.CommandLine", staticCommands, helpDocuments);

        var option = Assert.Single(document["options"]!.AsArray());
        var argument = Assert.Single(option!["arguments"]!.AsArray());
        Assert.Equal("PATH", argument!["name"]!.GetValue<string>());
        Assert.True(argument["required"]!.GetValue<bool>());
        Assert.Equal(1, argument["arity"]!["minimum"]!.GetValue<int>());
    }

    [Fact]
    public void Build_Falls_Back_To_Static_Surface_When_Help_Document_Has_Only_Descriptions()
    {
        var builder = new StaticAnalysisOpenCliBuilder();
        var staticCommands = new Dictionary<string, StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["copy"] = new(
                Name: "copy",
                Description: "Copy files.",
                IsDefault: false,
                IsHidden: false,
                Values:
                [
                    new StaticValueDefinition(0, "SOURCE", true, false, "System.String", "Source file.", null, []),
                ],
                Options:
                [
                    new StaticOptionDefinition("target", 't', true, false, false, "System.String", "Target file.", null, "TARGET", [], "Target"),
                ]),
        };
        var helpDocuments = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase)
        {
            ["copy"] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(description: "Copy files."),
        };

        var document = builder.Build("demo", "1.0.0", "System.CommandLine", staticCommands, helpDocuments);

        var rootCommands = Assert.IsType<System.Text.Json.Nodes.JsonArray>(document["commands"]);
        var copy = Assert.IsType<System.Text.Json.Nodes.JsonObject>(Assert.Single(rootCommands));
        Assert.Equal("copy", copy["name"]!.GetValue<string>());
        Assert.Contains(copy["options"]!.AsArray(), option => option?["name"]?.GetValue<string>() == "--target");
        Assert.Contains(copy["arguments"]!.AsArray(), argument => argument?["name"]?.GetValue<string>() == "SOURCE");
    }
}
