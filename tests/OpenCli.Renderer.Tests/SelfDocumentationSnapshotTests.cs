using System.Text.Json.Nodes;
using System.Xml.Linq;
using OpenCli.Renderer.Tests.TestSupport;

namespace OpenCli.Renderer.Tests;

public class SelfDocumentationSnapshotTests
{
    [Fact]
    public void Opencli_snapshot_keeps_html_commands_bundle_only()
    {
        var path = Path.Combine(FixturePaths.RepoRoot, "docs", "opencli-renderer", "opencli.json");
        var document = JsonNode.Parse(File.ReadAllText(path))!.AsObject();

        AssertHtmlCommand(document, "file");
        AssertHtmlCommand(document, "exec");
    }

    [Fact]
    public void Xmldoc_snapshot_keeps_html_settings_types_and_options_in_sync()
    {
        var path = Path.Combine(FixturePaths.RepoRoot, "docs", "opencli-renderer", "xmldoc.xml");
        var model = XDocument.Load(path);

        var fileHtml = FindHtmlCommand(model, "file");
        var execHtml = FindHtmlCommand(model, "exec");

        Assert.Equal("OpenCli.Renderer.Commands.Render.FileHtmlSettings", fileHtml.Attribute("Settings")?.Value);
        Assert.Equal("OpenCli.Renderer.Commands.Render.ExecHtmlSettings", execHtml.Attribute("Settings")?.Value);

        AssertHtmlOptions(fileHtml);
        AssertHtmlOptions(execHtml);
    }

    private static void AssertHtmlCommand(JsonObject document, string branchName)
    {
        var render = document["commands"]!.AsArray()
            .Single(command => command!["name"]!.GetValue<string>() == "render")!
            .AsObject();
        var branch = render["commands"]!.AsArray()
            .Single(command => command!["name"]!.GetValue<string>() == branchName)!;
        var html = branch["commands"]!.AsArray()
            .Single(command => command!["name"]!.GetValue<string>() == "html")!
            .AsObject();

        var optionNames = html["options"]!.AsArray()
            .Select(option => option!["name"]!.GetValue<string>())
            .ToArray();

        Assert.Contains("--out-dir", optionNames);
        Assert.DoesNotContain("--out", optionNames);
        Assert.DoesNotContain("--layout", optionNames);
        Assert.Contains("HTML app bundle", html["description"]!.GetValue<string>());
    }

    private static XElement FindHtmlCommand(XDocument model, string branchName)
    {
        var render = model.Root!.Elements("Command")
            .Single(command => command.Attribute("Name")?.Value == "render");
        var branch = render.Elements("Command")
            .Single(command => command.Attribute("Name")?.Value == branchName);

        return branch.Elements("Command")
            .Single(command => command.Attribute("Name")?.Value == "html");
    }

    private static void AssertHtmlOptions(XElement command)
    {
        var optionNames = command
            .Descendants("Option")
            .Select(option => option.Attribute("Long")?.Value)
            .Where(value => value is not null)
            .ToArray();

        Assert.Contains("out-dir", optionNames);
        Assert.DoesNotContain("out", optionNames);
        Assert.DoesNotContain("layout", optionNames);
    }
}
