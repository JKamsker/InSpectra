namespace InSpectra.Gen.Engine.Tests.OpenCli;

using InSpectra.Gen.Engine.Tooling.DocumentPipeline.Documents;

using System.Text.Json.Nodes;

public sealed class OpenCliDocumentSanitizerOptionCollisionTests
{
    [Fact]
    public void Sanitize_Merges_WellKnown_Informational_Option_By_Name_When_Descriptions_Drift()
    {
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "--version",
                ["description"] = "Display Retype information",
                ["aliases"] = new JsonArray("-v"),
            },
            new JsonObject
            {
                ["name"] = "--version",
                ["description"] = "Retype build information",
                ["arguments"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["required"] = false,
                        ["arity"] = new JsonObject
                        {
                            ["minimum"] = 0,
                            ["maximum"] = 1,
                        },
                        ["type"] = "Boolean",
                    },
                },
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        var version = Assert.Single(document["options"]!.AsArray())!;
        Assert.Equal("--version", version["name"]?.GetValue<string>());
        Assert.Equal("Display Retype information", version["description"]?.GetValue<string>());
        Assert.Equal("-v", Assert.Single(version["aliases"]!.AsArray())!.GetValue<string>());
        Assert.Null(version["arguments"]);
    }

    [Fact]
    public void Sanitize_Does_Not_Merge_Different_Informational_Options_That_Only_Share_A_Primary_Name()
    {
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "--help",
                ["aliases"] = new JsonArray("-h"),
                ["description"] = "Show help information.",
            },
            new JsonObject
            {
                ["name"] = "--help",
                ["aliases"] = new JsonArray("/?"),
                ["description"] = "Show help information.",
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        Assert.Equal(2, document["options"]!.AsArray().Count);
    }

    [Fact]
    public void Sanitize_Merges_Localized_Informational_Option_With_Optional_Boolean_Duplicate()
    {
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "--version",
                ["description"] = "Versionsinformationen anzeigen",
            },
            new JsonObject
            {
                ["name"] = "--version",
                ["description"] = "Show version information.",
                ["aliases"] = new JsonArray("-v"),
                ["arguments"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["required"] = false,
                        ["arity"] = new JsonObject
                        {
                            ["minimum"] = 0,
                            ["maximum"] = 1,
                        },
                        ["type"] = "Boolean",
                    },
                },
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        var version = Assert.Single(document["options"]!.AsArray())!.AsObject();
        Assert.Equal("--version", version["name"]?.GetValue<string>());
        Assert.Equal("Versionsinformationen anzeigen", version["description"]?.GetValue<string>());
        Assert.Equal("-v", Assert.Single(version["aliases"]!.AsArray())!.GetValue<string>());
        Assert.False(version.ContainsKey("arguments"));
    }

    [Fact]
    public void Sanitize_Promotes_Derived_Long_Name_When_Duplicate_Primary_Option_Names_Collide()
    {
        var document = new JsonObject
        {
            ["opencli"] = "0.1-draft",
            ["info"] = new JsonObject
            {
                ["title"] = "demo",
                ["version"] = "1.0.0",
            },
            ["commands"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "g2c",
                    ["options"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "--output",
                            ["aliases"] = new JsonArray("-o"),
                            ["description"] = "The implementation file to create.",
                            ["arguments"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["name"] = "OUTPUT-FILENAME",
                                    ["required"] = true,
                                },
                            },
                        },
                        new JsonObject
                        {
                            ["name"] = "--output",
                            ["aliases"] = new JsonArray("-on"),
                            ["description"] = "The target namespace for the output.",
                            ["arguments"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["name"] = "OUTPUT-NAMESPACE",
                                    ["required"] = true,
                                },
                            },
                        },
                    },
                },
            },
        };

        OpenCliDocumentSanitizer.Sanitize(document);

        var options = document["commands"]![0]!["options"]!.AsArray();
        Assert.Equal("--output", options[0]!["name"]?.GetValue<string>());
        Assert.Equal("--output-namespace", options[1]!["name"]?.GetValue<string>());
        Assert.Equal("-on", Assert.Single(options[1]!["aliases"]!.AsArray())!.GetValue<string>());
    }

    [Fact]
    public void Sanitize_Does_Not_Hide_WellKnown_Informational_Primary_Name_Collisions()
    {
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "--version",
                ["aliases"] = new JsonArray("-v"),
                ["description"] = "Will cause the application to mark all project files with this version number",
            },
            new JsonObject
            {
                ["name"] = "--version",
                ["description"] = "Display version information.",
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        var options = document["options"]!.AsArray();
        Assert.Equal(2, options.Count);
        Assert.All(
            options.OfType<JsonObject>(),
            option => Assert.Equal("--version", option["name"]?.GetValue<string>()));
    }

    [Fact]
    public void Sanitize_Does_Not_Merge_Custom_Version_Self_Argument_With_BuiltIn_Version_Row()
    {
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "--version",
                ["aliases"] = new JsonArray("-v"),
                ["description"] = "Will cause the application to mark all project files with this version number",
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

        var options = document["options"]!.AsArray();
        Assert.Equal(2, options.Count);
        Assert.Contains(
            options.OfType<JsonObject>(),
            option => option["arguments"] is JsonArray);
        Assert.Contains(
            options.OfType<JsonObject>(),
            option => string.Equals(option["description"]?.GetValue<string>(), "Display version information.", StringComparison.Ordinal));
    }

    [Fact]
    public void Sanitize_Merges_Well_Known_Informational_Duplicate_When_One_Token_Set_Is_Subset()
    {
        // Hook synthetic injection produces --version (no alias).
        // Framework built-in produces --version with -v alias.
        // Both are informational. Token sets: {--version} ⊂ {--version, -v}.
        // Should merge into the richer entry.
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "--version",
                ["description"] = "Display version information.",
            },
            new JsonObject
            {
                ["name"] = "--version",
                ["description"] = "Display version information.",
                ["aliases"] = new JsonArray("-v"),
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        var version = Assert.Single(document["options"]!.AsArray());
        Assert.Equal("--version", version!["name"]?.GetValue<string>());
        Assert.Contains(version["aliases"]!.AsArray(), a => a?.GetValue<string>() == "-v");
    }

    [Fact]
    public void Sanitize_Merges_Identical_Well_Known_Informational_Duplicates_Without_Aliases()
    {
        // Both from the same framework — identical --help entries, no aliases.
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "--help",
                ["description"] = "Display this help screen.",
            },
            new JsonObject
            {
                ["name"] = "--help",
                ["description"] = "Display this help screen.",
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        Assert.Single(document["options"]!.AsArray());
    }

    private static JsonObject CreateDocument(params JsonObject[] options)
        => OpenCliDocumentSanitizerOptionMergeTests.CreateDocument(options);
}
