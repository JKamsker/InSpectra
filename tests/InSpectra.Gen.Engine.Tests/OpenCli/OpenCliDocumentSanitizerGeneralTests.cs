namespace InSpectra.Gen.Engine.Tests.OpenCli;

using InSpectra.Gen.Engine.Tooling.DocumentPipeline.Documents;
using InSpectra.Gen.Engine.Tooling.DocumentPipeline.Options;

using System.Text.Json.Nodes;

public sealed class OpenCliDocumentSanitizerGeneralTests
{
    [Fact]
    public void ApplyNuGetMetadata_Ignores_NonPublishable_NuGet_Title()
    {
        var document = new JsonObject
        {
            ["opencli"] = "0.1-draft",
            ["info"] = new JsonObject
            {
                ["title"] = "weixin",
                ["version"] = "0.1.1",
            },
        };

        OpenCliDocumentSanitizer.ApplyNuGetMetadata(document, "Senparc.WebSocket.dll", null);

        Assert.Equal("weixin", document["info"]?["title"]?.GetValue<string>());
    }

    [Fact]
    public void Sanitize_Removes_Null_Optional_Fields_Empty_Examples_And_Option_Required()
    {
        var document = new JsonObject
        {
            ["opencli"] = "0.1-draft",
            ["info"] = new JsonObject
            {
                ["title"] = "demo",
                ["version"] = "1.0.0",
                ["description"] = null,
            },
            ["options"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "--verbose",
                    ["required"] = false,
                    ["description"] = null,
                    ["aliases"] = new JsonArray(),
                },
                new JsonObject
                {
                    ["name"] = "--count",
                    ["required"] = false,
                    ["arguments"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "VALUE",
                            ["required"] = true,
                            ["arity"] = new JsonObject
                            {
                                ["minimum"] = 1,
                                ["maximum"] = 1,
                            },
                        },
                    },
                },
            },
            ["commands"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "serve",
                    ["description"] = null,
                    ["arguments"] = null,
                    ["options"] = new JsonArray(),
                    ["examples"] = new JsonArray(),
                },
            },
        };

        OpenCliDocumentSanitizer.EnsureArtifactSource(document, "tool-output");
        OpenCliDocumentSanitizer.Sanitize(document);

        Assert.Equal("tool-output", document["x-inspectra"]?["artifactSource"]?.GetValue<string>());
        Assert.False(document["info"]!.AsObject().ContainsKey("description"));

        var verbose = document["options"]![0]!.AsObject();
        Assert.False(verbose.ContainsKey("required"));
        Assert.False(verbose.ContainsKey("description"));
        Assert.False(verbose.ContainsKey("aliases"));

        var count = document["options"]![1]!.AsObject();
        Assert.False(count.ContainsKey("required"));
        Assert.True(count["arguments"]![0]!["required"]!.GetValue<bool>());

        var serve = document["commands"]![0]!.AsObject();
        Assert.False(serve.ContainsKey("description"));
        Assert.False(serve.ContainsKey("arguments"));
        Assert.False(serve.ContainsKey("options"));
        Assert.False(serve.ContainsKey("examples"));
    }

    [Fact]
    public void Sanitize_Preserves_Required_True_On_Options()
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
                    ["name"] = "generate",
                    ["options"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "--output",
                            ["required"] = true,
                            ["description"] = "Output file path",
                        },
                        new JsonObject
                        {
                            ["name"] = "--verbose",
                            ["required"] = false,
                            ["description"] = "Enable verbose output",
                        },
                    },
                },
            },
        };

        OpenCliDocumentSanitizer.Sanitize(document);

        var options = document["commands"]![0]!["options"]!.AsArray();
        var output = options.First(o => o!["name"]?.GetValue<string>() == "--output")!.AsObject();
        var verbose = options.First(o => o!["name"]?.GetValue<string>() == "--verbose")!.AsObject();

        // required: true should be preserved — it carries semantic meaning
        Assert.True(output.ContainsKey("required"));
        Assert.True(output["required"]!.GetValue<bool>());

        // required: false should still be stripped — it's the default
        Assert.False(verbose.ContainsKey("required"));
    }

    [Fact]
    public void Sanitize_Preserves_Gnu_Style_Value_Hint_Aliases()
    {
        var document = new JsonObject
        {
            ["opencli"] = "0.1-draft",
            ["info"] = new JsonObject
            {
                ["title"] = "demo",
                ["version"] = "1.0.0",
            },
            ["options"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "--output",
                    ["aliases"] = new JsonArray("-o", "--output=<FILENAME>"),
                    ["description"] = "Output file path",
                },
            },
        };

        OpenCliDocumentSanitizer.Sanitize(document);

        var option = Assert.Single(document["options"]!.AsArray());
        Assert.Equal("--output", option!["name"]?.GetValue<string>());
        // The =<FILENAME> suffix should be stripped during normalization,
        // leaving --output as the normalized base (which deduplicates with the
        // primary name). The -o alias must survive.
        var aliases = option["aliases"]?.AsArray()
            .Select(a => a?.GetValue<string>())
            .ToArray() ?? [];
        Assert.Contains("-o", aliases);
        // The raw --output=<FILENAME> should NOT survive as-is
        Assert.DoesNotContain("--output=<FILENAME>", aliases);
    }

    [Fact]
    public void Sanitize_Preserves_Option_With_Only_Gnu_Style_Alias()
    {
        // migrator.database.cli case: --output has aliases -o AND --output=<FILENAME>.
        // When --output=<FILENAME> is the only long-form alias distinguishing a second
        // --output variant, stripping it must not silently drop the entire option.
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
                    ["name"] = "migrate",
                    ["options"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "--output=<FILENAME>",
                            ["description"] = "Write output to file",
                        },
                    },
                },
            },
        };

        OpenCliDocumentSanitizer.Sanitize(document);

        var options = document["commands"]![0]!["options"]!.AsArray();
        var option = Assert.Single(options);
        // The =<FILENAME> suffix should be stripped, leaving --output as the name
        Assert.Equal("--output", option!["name"]?.GetValue<string>());
    }

    [Fact]
    public void Sanitize_Merges_Same_Option_When_Descriptions_Are_NearEquivalent()
    {
        var document = new JsonObject
        {
            ["opencli"] = "0.1-draft",
            ["info"] = new JsonObject
            {
                ["title"] = "demo",
                ["version"] = "1.0.0",
            },
            ["options"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "--input",
                    ["description"] = "Parse the given string as input.",
                    ["aliases"] = new JsonArray("-i"),
                },
                new JsonObject
                {
                    ["name"] = "--input",
                    ["description"] = "Parse input string.",
                    ["aliases"] = new JsonArray("-i"),
                },
            },
        };

        OpenCliDocumentSanitizer.Sanitize(document);

        var input = Assert.Single(document["options"]!.AsArray());
        Assert.Equal("--input", input!["name"]?.GetValue<string>());
        Assert.Contains(input["aliases"]!.AsArray(), alias => alias?.GetValue<string>() == "-i");
    }
}
