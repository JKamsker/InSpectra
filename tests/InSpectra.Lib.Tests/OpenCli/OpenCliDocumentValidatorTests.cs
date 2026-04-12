namespace InSpectra.Lib.Tests.OpenCli;

using InSpectra.Lib.Tooling.DocumentPipeline.Documents;

using System.Text.Json.Nodes;

public sealed class OpenCliDocumentValidatorTests
{
    [Fact]
    public void TryValidateDocument_Rejects_Child_Command_Without_Name()
    {
        var document = CreateValidDocument();
        document["commands"] = new JsonArray
        {
            new JsonObject
            {
                ["description"] = "Unnamed command",
            },
        };

        var isValid = OpenCliDocumentValidator.TryValidateDocument(document, out var reason);

        Assert.False(isValid);
        Assert.Contains("missing a command name", reason);
    }

    [Fact]
    public void TryValidateDocument_Rejects_Option_Without_Name()
    {
        var document = CreateValidDocument();
        document["options"] = new JsonArray
        {
            new JsonObject
            {
                ["description"] = "Unnamed option",
            },
        };

        var isValid = OpenCliDocumentValidator.TryValidateDocument(document, out var reason);

        Assert.False(isValid);
        Assert.Contains("missing a option name", reason);
    }

    [Fact]
    public void TryValidateDocument_Rejects_Argument_Without_Name()
    {
        var document = CreateValidDocument();
        document["arguments"] = new JsonArray
        {
            new JsonObject
            {
                ["description"] = "Unnamed argument",
            },
        };

        var isValid = OpenCliDocumentValidator.TryValidateDocument(document, out var reason);

        Assert.False(isValid);
        Assert.Contains("missing a argument name", reason);
    }

    private static JsonObject CreateValidDocument()
        => new()
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
                    ["name"] = "sync",
                    ["description"] = "Synchronize content.",
                },
            },
        };
}
