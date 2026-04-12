namespace InSpectra.Lib.Tests.StaticAnalysis;

using InSpectra.Lib.Contracts.Documents;
using InSpectra.Lib.Modes.Static.Metadata;
using InSpectra.Lib.Modes.Static.Projection;

using System.Text.Json.Nodes;

public sealed class StaticAnalysisOpenCliBuilderStructureTests
{
    [Fact]
    public void Build_Nests_MultiSegment_Help_Commands_And_Preserves_Leaf_Inputs()
    {
        var builder = new StaticAnalysisOpenCliBuilder();
        var helpDocuments = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(
                commands: [new Item("config", false, "Configuration commands")]),
            ["config"] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(
                description: "Configuration commands",
                commands: [new Item("credentials", false, "Manage credentials")]),
            ["config credentials"] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(
                description: "Manage credentials",
                commands: [new Item("set", false, "Set a credential")]),
            ["config credentials set"] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(
                description: "Set a credential",
                options:
                [
                    new Item("--key", true, "Credential key"),
                    new Item("--value", true, "Credential value"),
                ]),
        };

        var document = builder.Build(
            "workbench",
            "1.0.0",
            "System.CommandLine",
            new Dictionary<string, StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase),
            helpDocuments);

        var rootCommands = Assert.IsType<JsonArray>(document["commands"]);
        var config = Assert.IsType<JsonObject>(Assert.Single(rootCommands));
        Assert.Equal("config", config["name"]!.GetValue<string>());
        var configCommands = Assert.IsType<JsonArray>(config["commands"]);
        var credentials = Assert.IsType<JsonObject>(Assert.Single(configCommands));
        Assert.Equal("credentials", credentials["name"]!.GetValue<string>());
        var credentialCommands = Assert.IsType<JsonArray>(credentials["commands"]);
        var set = Assert.IsType<JsonObject>(Assert.Single(credentialCommands));
        Assert.Equal("set", set["name"]!.GetValue<string>());
        Assert.Equal("Set a credential", set["description"]!.GetValue<string>());
        Assert.Contains(set["options"]!.AsArray(), option => option?["name"]?.GetValue<string>() == "--key");
        Assert.Contains(set["options"]!.AsArray(), option => option?["name"]?.GetValue<string>() == "--value");
    }

    [Fact]
    public void Build_Synthesizes_Intermediate_Parents_For_Static_Only_MultiSegment_Commands()
    {
        var builder = new StaticAnalysisOpenCliBuilder();
        var staticCommands = new Dictionary<string, StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["deploy release create"] = new(
                Name: "deploy release create",
                Description: "Create a release deployment.",
                IsDefault: false,
                IsHidden: false,
                Values: [],
                Options:
                [
                    new StaticOptionDefinition(
                        LongName: "target",
                        ShortName: 't',
                        IsRequired: true,
                        IsSequence: false,
                        IsBoolLike: false,
                        ClrType: "System.String",
                        Description: "Deployment target.",
                        DefaultValue: null,
                        MetaValue: "TARGET",
                        AcceptedValues: [],
                        PropertyName: "Target"),
                ]),
        };

        var document = builder.Build(
            "deployer",
            "1.0.0",
            "CommandLineParser",
            staticCommands,
            new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase));

        var rootCommands = Assert.IsType<JsonArray>(document["commands"]);
        var deploy = Assert.IsType<JsonObject>(Assert.Single(rootCommands));
        Assert.Equal("deploy", deploy["name"]!.GetValue<string>());
        var deployCommands = Assert.IsType<JsonArray>(deploy["commands"]);
        var release = Assert.IsType<JsonObject>(Assert.Single(deployCommands));
        Assert.Equal("release", release["name"]!.GetValue<string>());
        var releaseCommands = Assert.IsType<JsonArray>(release["commands"]);
        var create = Assert.IsType<JsonObject>(Assert.Single(releaseCommands));
        Assert.Equal("create", create["name"]!.GetValue<string>());
        Assert.Equal("Create a release deployment.", create["description"]!.GetValue<string>());
        Assert.Contains(create["options"]!.AsArray(), option => option?["name"]?.GetValue<string>() == "--target");
    }

    [Fact]
    public void Build_Prefers_Help_Surface_Over_Unmatched_Static_Metadata_When_Help_Graph_Exists()
    {
        var builder = new StaticAnalysisOpenCliBuilder();
        var staticCommands = new Dictionary<string, StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = new(
                Name: null,
                Description: "Default command",
                IsDefault: true,
                IsHidden: false,
                Values:
                [
                    new StaticValueDefinition(0, "FILE", true, false, "System.String", "Static-only positional argument.", null, []),
                ],
                Options:
                [
                    new StaticOptionDefinition(
                        LongName: "value",
                        ShortName: 'v',
                        IsRequired: true,
                        IsSequence: false,
                        IsBoolLike: false,
                        ClrType: "System.String",
                        Description: "Static-only root option.",
                        DefaultValue: null,
                        MetaValue: "VALUE",
                        AcceptedValues: [],
                        PropertyName: "Value"),
                ]),
            ["greet"] = new("greet", "Greet a user.", false, false, [], []),
            ["gethashcode"] = new("gethashcode", "Compiler noise.", false, false, [], []),
            ["<clone>$"] = new("<clone>$", "Compiler noise.", false, false, [], []),
        };
        var helpDocuments = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(
                title: "demo",
                version: "1.0.0",
                options:
                [
                    new Item("-h, --help", false, "Show help."),
                    new Item("--version", false, "Show version."),
                ],
                commands: [new Item("greet", false, "Greet a user.")]),
            ["greet"] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(description: "Greet a user."),
        };

        var document = builder.Build("demo", "1.0.0", "Cocona", staticCommands, helpDocuments);

        var commands = Assert.IsType<JsonArray>(document["commands"]);
        var greet = Assert.IsType<JsonObject>(Assert.Single(commands));
        Assert.Equal("greet", greet["name"]!.GetValue<string>());
        var options = document["options"]!.AsArray()
            .OfType<JsonObject>()
            .Select(option => option["name"]!.GetValue<string>())
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["--help", "--version"], options);
        Assert.Null(document["arguments"]);
    }

    [Fact]
    public void Build_Keeps_Static_Descendants_When_Help_Graph_Only_Covers_The_Ancestor()
    {
        var builder = new StaticAnalysisOpenCliBuilder();
        var staticCommands = new Dictionary<string, StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["foo bar"] = new("foo bar", "Run the bar workflow.", false, false, [], []),
        };
        var helpDocuments = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(
                commands: [new Item("foo", false, "Foo commands")]),
        };

        var document = builder.Build("demo", "1.0.0", "System.CommandLine", staticCommands, helpDocuments);

        var rootCommands = Assert.IsType<JsonArray>(document["commands"]);
        var foo = Assert.IsType<JsonObject>(Assert.Single(rootCommands));
        Assert.Equal("foo", foo["name"]!.GetValue<string>());
        var fooCommands = Assert.IsType<JsonArray>(foo["commands"]);
        var bar = Assert.IsType<JsonObject>(Assert.Single(fooCommands));
        Assert.Equal("bar", bar["name"]!.GetValue<string>());
        Assert.Equal("Run the bar workflow.", bar["description"]!.GetValue<string>());
    }

    [Fact]
    public void Build_Preserves_Variadic_Arity_When_Help_Anchors_Static_Arguments()
    {
        var builder = new StaticAnalysisOpenCliBuilder();
        var staticCommands = new Dictionary<string, StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = new(
                Name: null,
                Description: "Process paths.",
                IsDefault: true,
                IsHidden: false,
                Values:
                [
                    new StaticValueDefinition(0, "paths", true, true, "System.String[]", "Paths to process.", null, []),
                ],
                Options: []),
        };
        var helpDocuments = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(
                arguments: [new Item("paths", true, "Paths to process.")]),
        };

        var document = builder.Build("demo", "1.0.0", "System.CommandLine", staticCommands, helpDocuments);

        var arguments = Assert.IsType<JsonArray>(document["arguments"]);
        var argument = Assert.IsType<JsonObject>(Assert.Single(arguments));
        Assert.Equal("paths", argument["name"]!.GetValue<string>());
        var arity = Assert.IsType<JsonObject>(argument["arity"]);
        Assert.Equal(1, arity["minimum"]!.GetValue<int>());
        Assert.Null(arity["maximum"]);
    }

    [Fact]
    public void Build_Help_Anchored_Arguments_Fall_Back_To_Static_Requiredness_And_Description()
    {
        var builder = new StaticAnalysisOpenCliBuilder();
        var staticCommands = new Dictionary<string, StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = new(
                Name: null,
                Description: "Process input.",
                IsDefault: true,
                IsHidden: false,
                Values:
                [
                    new StaticValueDefinition(0, "input", true, false, "System.String", "Input file.", null, []),
                ],
                Options: []),
        };
        var helpDocuments = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(
                arguments: [new Item("input", false, null)]),
        };

        var document = builder.Build("demo", "1.0.0", "System.CommandLine", staticCommands, helpDocuments);

        var arguments = Assert.IsType<JsonArray>(document["arguments"]);
        var argument = Assert.IsType<JsonObject>(Assert.Single(arguments));
        Assert.Equal("input", argument["name"]!.GetValue<string>());
        Assert.True(argument["required"]!.GetValue<bool>());
        Assert.Equal("Input file.", argument["description"]!.GetValue<string>());
    }

    [Fact]
    public void Build_Emits_Help_Only_Root_Commands_When_Root_Also_Has_Global_Options()
    {
        var builder = new StaticAnalysisOpenCliBuilder();
        var helpDocuments = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = StaticAnalysisOpenCliBuilderTestSupport.CreateHelpDocument(
                options:
                [
                    new Item("--help", false, "Show help."),
                    new Item("--version", false, "Show version."),
                ],
                commands: [new Item("greet", false, "Greet a user.")]),
        };

        var document = builder.Build(
            "demo",
            "1.0.0",
            "System.CommandLine",
            new Dictionary<string, StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase),
            helpDocuments);

        var rootCommands = Assert.IsType<JsonArray>(document["commands"]);
        var greet = Assert.IsType<JsonObject>(Assert.Single(rootCommands));
        Assert.Equal("greet", greet["name"]!.GetValue<string>());
        var options = Assert.IsType<JsonArray>(document["options"]);
        Assert.Equal(2, options.Count);
    }
}
