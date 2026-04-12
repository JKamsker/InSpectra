namespace InSpectra.Gen.Engine.Tests.Help;

using InSpectra.Gen.Engine.Modes.Help.Crawling;
using InSpectra.Gen.Engine.Modes.Help.Parsing;

using System.Text.Json.Nodes;

public sealed class CapturePayloadSupportTests
{
    [Fact]
    public void SelectBestDocument_Rejects_Sibling_Dispatcher_Echo_Without_Leaf_Surface()
    {
        var parser = new TextParser();
        var capture = new JsonObject
        {
            ["command"] = "config",
            ["payload"] =
            """
            demo

            USAGE
              demo other [command]

            COMMANDS
              show  Show values.
            """,
        };

        var selected = CapturePayloadSupport.SelectBestDocument(parser, "demo", capture);

        Assert.Null(selected);
    }

    [Fact]
    public void SelectBestDocument_Keeps_Mixed_Leaf_Surface_Capture()
    {
        var parser = new TextParser();
        var capture = new JsonObject
        {
            ["command"] = "config",
            ["payload"] =
            """
            demo

            USAGE
              demo other [command]

            OPTIONS
              --value  Configuration value.

            COMMANDS
              show  Show values.
            """,
        };

        var selected = CapturePayloadSupport.SelectBestDocument(parser, "demo", capture);

        Assert.NotNull(selected);
        Assert.Equal("config", selected!.CommandKey);
        Assert.Contains(selected.Document.Options, option => option.Key == "--value");
    }
}
