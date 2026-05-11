namespace InSpectra.Lib.Tests.OpenCli;

using InSpectra.Lib.Tooling.DocumentPipeline.Documents;

using System.Text.Json.Nodes;

public sealed class OpenCliOptionDuplicatePrimaryMergeTests
{
    [Fact]
    public void Sanitize_Merges_Duplicate_Primary_Token_When_One_Row_Is_Richer()
    {
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "--dry-run",
            },
            new JsonObject
            {
                ["name"] = "--dry-run",
                ["description"] = "Show what would happen without making changes.",
                ["aliases"] = new JsonArray("-n"),
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        var option = Assert.Single(document["options"]!.AsArray())!.AsObject();
        Assert.Equal("--dry-run", option["name"]?.GetValue<string>());
        Assert.Equal("Show what would happen without making changes.", option["description"]?.GetValue<string>());
        Assert.Equal("-n", Assert.Single(option["aliases"]!.AsArray())!.GetValue<string>());
    }

    [Fact]
    public void Sanitize_Merges_Duplicate_Primary_Token_With_Missing_Argument_Detail()
    {
        var document = CreateDocument(
            new JsonObject
            {
                ["name"] = "/property",
                ["description"] = "Set or override project properties.",
                ["arguments"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "NAME=VALUE",
                        ["required"] = true,
                    },
                },
            },
            new JsonObject
            {
                ["name"] = "/property",
                ["description"] = "Set project properties.",
            });

        OpenCliDocumentSanitizer.Sanitize(document);

        var option = Assert.Single(document["options"]!.AsArray())!.AsObject();
        Assert.Equal("/property", option["name"]?.GetValue<string>());
        Assert.Equal("NAME=VALUE", option["arguments"]![0]!["name"]?.GetValue<string>());
    }

    private static JsonObject CreateDocument(params JsonObject[] options)
        => OpenCliDocumentSanitizerOptionMergeTests.CreateDocument(options);
}
