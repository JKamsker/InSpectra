namespace InSpectra.Gen.Engine.Tests.OpenCli;

using InSpectra.Gen.Engine.Tooling.DocumentPipeline.Documents;

using System.Text.Json.Nodes;

public sealed class OpenCliDocumentSanitizerOptionMergeTests
{
    [Fact]
    public void Sanitize_Merges_Informational_Option_When_Trailing_Positional_Noise_Is_Present()
    {
        var document = CreateDocument(
            new JsonObject { ["name"] = "--version" },
            new JsonObject
            {
                ["name"] = "--version",
                ["description"] = "Display version information.\nvalue pos. 0",
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        var version = Assert.Single(document["options"]!.AsArray());
        Assert.Equal("--version", version!["name"]?.GetValue<string>());
        Assert.Equal("Display version information.", version["description"]?.GetValue<string>());
    }

    [Fact]
    public void Sanitize_Does_Not_Merge_Informational_Option_With_Value_Taking_Duplicate()
    {
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "--version",
                ["description"] = "Package version to publish.",
                ["arguments"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "VERSION",
                        ["required"] = true,
                        ["arity"] = new JsonObject
                        {
                            ["minimum"] = 1,
                            ["maximum"] = 1,
                        },
                    },
                },
            },
            new JsonObject
            {
                ["name"] = "--version",
                ["description"] = "Display version information.",
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        var options = document["options"]!.AsArray();
        Assert.Equal(2, options.Count);
        Assert.Contains(options, option => option?["arguments"] is JsonArray);
        Assert.Contains(options, option => option?["description"]?.GetValue<string>() == "Display version information.");
    }

    [Fact]
    public void Sanitize_Merges_Standalone_Alias_Into_Richer_Option()
    {
        var document = CreateDocument(
            new JsonObject { ["name"] = "-c" },
            new JsonObject
            {
                ["name"] = "--config",
                ["aliases"] = new JsonArray("-c"),
                ["description"] = "Configuration file to load.",
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        var config = Assert.Single(document["options"]!.AsArray());
        Assert.Equal("--config", config!["name"]?.GetValue<string>());
        Assert.Equal("Configuration file to load.", config["description"]?.GetValue<string>());
        Assert.Contains(config["aliases"]!.AsArray(), alias => alias?.GetValue<string>() == "-c");
    }

    [Fact]
    public void Sanitize_Merges_Same_Option_With_Alternative_Value_Forms()
    {
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "--regex",
                ["description"] = "Read regex patterns from FILE (one per line)",
                ["arguments"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "FILE",
                        ["required"] = true,
                        ["arity"] = new JsonObject
                        {
                            ["minimum"] = 1,
                            ["maximum"] = 1,
                        },
                    },
                },
            },
            new JsonObject
            {
                ["name"] = "--regex",
                ["description"] = "Regex pattern(s) that should match (can specify multiple)",
                ["arguments"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "PATTERN",
                        ["required"] = true,
                        ["arity"] = new JsonObject
                        {
                            ["minimum"] = 1,
                            ["maximum"] = 1,
                        },
                    },
                },
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        var regex = Assert.Single(document["options"]!.AsArray())!.AsObject();
        Assert.Equal("--regex", regex["name"]?.GetValue<string>());
        Assert.Equal("REGEX", regex["arguments"]![0]!["name"]?.GetValue<string>());
        Assert.Contains("Read regex patterns from FILE", regex["description"]?.GetValue<string>(), StringComparison.Ordinal);
        Assert.Contains("Regex pattern(s) that should match", regex["description"]?.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public void Sanitize_Merges_Informational_Option_With_Synthetic_Self_Argument_Duplicate()
    {
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "--version",
                ["arguments"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "VERSION",
                        ["required"] = false,
                        ["arity"] = new JsonObject
                        {
                            ["minimum"] = 0,
                            ["maximum"] = 1,
                        },
                    },
                },
            },
            new JsonObject
            {
                ["name"] = "--version",
                ["description"] = "Display version information.",
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        var version = Assert.Single(document["options"]!.AsArray());
        Assert.Equal("--version", version!["name"]?.GetValue<string>());
        Assert.Equal("Display version information.", version["description"]?.GetValue<string>());
        Assert.Null(version["arguments"]);
    }

    [Fact]
    public void Sanitize_Trims_Trailing_Noise_From_Single_Informational_Option()
    {
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "--version",
                ["description"] = "Display version information.\nvalue pos. 0",
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        var version = Assert.Single(document["options"]!.AsArray());
        Assert.Equal("--version", version!["name"]?.GetValue<string>());
        Assert.Equal("Display version information.", version["description"]?.GetValue<string>());
    }

    [Fact]
    public void Sanitize_Prefers_Richer_Compatible_Option_Description_When_Merging()
    {
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "--config",
                ["description"] = "JSON file containing XAML Styler settings",
                ["aliases"] = new JsonArray("-c"),
            },
            new JsonObject
            {
                ["name"] = "--config",
                ["description"] = "JSON file containing XAML Styler settings\nconfiguration.",
                ["aliases"] = new JsonArray("-c"),
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        var config = Assert.Single(document["options"]!.AsArray());
        Assert.Equal(
            "JSON file containing XAML Styler settings\nconfiguration.",
            config!["description"]?.GetValue<string>());
    }

    internal static JsonObject CreateDocument(params JsonObject[] options)
        => new()
        {
            ["opencli"] = "0.1-draft",
            ["info"] = new JsonObject
            {
                ["title"] = "demo",
                ["version"] = "1.0.0",
            },
            ["options"] = new JsonArray(options.Cast<JsonNode>().ToArray()),
        };
}
