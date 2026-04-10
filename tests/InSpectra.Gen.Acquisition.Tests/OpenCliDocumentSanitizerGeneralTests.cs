namespace InSpectra.Gen.Acquisition.Tests;

using InSpectra.Discovery.Tool.OpenCli.Documents;

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
