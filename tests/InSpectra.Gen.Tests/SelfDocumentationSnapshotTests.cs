using System.Text.Json.Nodes;
using System.Xml.Linq;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests;

public class SelfDocumentationSnapshotTests
{
    [Fact]
    public void Opencli_snapshot_keeps_html_commands_bundle_only()
    {
        var path = Path.Combine(FixturePaths.RepoRoot, "docs", "inspectra-gen", "opencli.json");
        var document = JsonNode.Parse(File.ReadAllText(path))!.AsObject();

        AssertHtmlCommand(document, "file");
        AssertHtmlCommand(document, "exec");
    }

    [Fact]
    public void Xmldoc_snapshot_keeps_html_settings_types_and_options_in_sync()
    {
        var path = Path.Combine(FixturePaths.RepoRoot, "docs", "inspectra-gen", "xmldoc.xml");
        var model = XDocument.Load(path);

        var fileHtml = FindHtmlCommand(model, "file");
        var execHtml = FindHtmlCommand(model, "exec");

        Assert.Equal("InSpectra.Gen.Commands.Render.FileHtmlSettings", fileHtml.Attribute("Settings")?.Value);
        Assert.Equal("InSpectra.Gen.Commands.Render.ExecHtmlSettings", execHtml.Attribute("Settings")?.Value);

        AssertHtmlOptions(fileHtml);
        AssertHtmlOptions(execHtml);
    }

    [Fact]
    public void Xmldoc_snapshot_documents_every_cli_parameter()
    {
        var path = Path.Combine(FixturePaths.RepoRoot, "docs", "inspectra-gen", "xmldoc.xml");
        var model = XDocument.Load(path);

        var undocumented = model.Descendants()
            .Where(element => element.Name.LocalName is "Argument" or "Option")
            .Where(element => string.IsNullOrWhiteSpace(element.Element("Description")?.Value))
            .Select(DescribeParameter)
            .ToArray();

        Assert.True(
            undocumented.Length == 0,
            "Undocumented CLI parameters:" + Environment.NewLine + string.Join(Environment.NewLine, undocumented));
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

    private static string DescribeParameter(XElement parameter)
    {
        var commandPath = string.Join(
            " ",
            parameter.Ancestors("Command")
                .Reverse()
                .Select(command => command.Attribute("Name")?.Value)
                .Where(name => !string.IsNullOrWhiteSpace(name)));

        var label = parameter.Name.LocalName switch
        {
            "Argument" => parameter.Attribute("Name")?.Value,
            "Option" => "--" + parameter.Attribute("Long")?.Value,
            _ => parameter.Attribute("Name")?.Value,
        };

        return $"{commandPath} {label}".Trim();
    }
}
