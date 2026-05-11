namespace InSpectra.Discovery.Tool.Tests;

using InSpectra.Discovery.Tool.OpenCli.Documents;

using System.Text.Json.Nodes;

using Xunit;

public sealed class OpenCliOptionDuplicatePrimaryMergeTests
{
    [Fact]
    public void Sanitize_Merges_Duplicate_Primary_Token_When_One_Row_Is_Richer()
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
                    ["name"] = "--rules",
                },
                new JsonObject
                {
                    ["name"] = "--rules",
                    ["description"] = "Path to the rule configuration file.",
                    ["aliases"] = new JsonArray("-r"),
                    ["arguments"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "PATH",
                            ["required"] = true,
                        },
                    },
                },
            },
        };

        OpenCliDocumentSanitizer.Sanitize(document);

        var option = Assert.Single(document["options"]!.AsArray())!.AsObject();
        Assert.Equal("--rules", option["name"]?.GetValue<string>());
        Assert.Equal("-r", Assert.Single(option["aliases"]!.AsArray())!.GetValue<string>());
        Assert.Equal("PATH", option["arguments"]![0]!["name"]?.GetValue<string>());
    }
}
