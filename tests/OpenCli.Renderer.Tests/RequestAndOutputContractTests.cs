using System.Text.Json.Nodes;
using OpenCli.Renderer.Runtime;

namespace OpenCli.Renderer.Tests;

public class RequestAndOutputContractTests
{
    [Fact]
    public void Json_single_output_requires_out_file()
    {
        var settings = new TestMarkdownSettings
        {
            Json = true,
        };

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateMarkdownOptions(settings, "single", null, null, timeoutSeconds: null, hasTimeoutSupport: false));

        Assert.Contains("requires `--out`", exception.Message);
    }

    [Fact]
    public void Html_output_requires_out_dir()
    {
        var settings = new TestHtmlSettings();

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateHtmlOptions(settings, null, null, null, timeoutSeconds: null, hasTimeoutSupport: false));

        Assert.Contains("requires `--out-dir`", exception.Message);
    }

    [Fact]
    public void Html_output_rejects_out_file()
    {
        var settings = new TestHtmlSettings
        {
            OutputFile = "docs.html",
        };

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateHtmlOptions(settings, null, settings.OutputFile, null, timeoutSeconds: null, hasTimeoutSupport: false));

        Assert.Contains("`--out` is not supported", exception.Message);
    }

    [Fact]
    public void Html_output_rejects_layout()
    {
        var settings = new TestHtmlSettings
        {
            Layout = "tree",
            OutputDirectory = "docs",
        };

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateHtmlOptions(settings, settings.Layout, null, settings.OutputDirectory, timeoutSeconds: null, hasTimeoutSupport: false));

        Assert.Contains("`--layout` is not supported", exception.Message);
    }

    [Fact]
    public async Task Json_output_writer_emits_versioned_success_envelope()
    {
        var original = Console.Out;
        await using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            var result = new RenderExecutionResult
            {
                Format = DocumentFormat.Html,
                Layout = RenderLayout.App,
                Source = new RenderSourceInfo("file", "sample.json", null, null),
                Stats = new RenderStats(1, 2, 3, 1),
                Warnings = [],
                IsDryRun = false,
                Files = [new RenderedFile("index.html", "C:\\temp\\index.html", null)],
                Summary = null,
            };

            var exitCode = await CommandOutputHandler.ExecuteAsync(
                ResolvedOutputMode.Json,
                verbose: false,
                () => Task.FromResult(result));

            var json = JsonNode.Parse(writer.ToString());

            Assert.Equal(0, exitCode);
            Assert.NotNull(json);
            Assert.True(json!["ok"]!.GetValue<bool>());
            Assert.Equal("html", json["data"]!["format"]!.GetValue<string>());
            Assert.Equal("app", json["data"]!["layout"]!.GetValue<string>());
            Assert.Equal(1, json["meta"]!["schemaVersion"]!.GetValue<int>());
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    private sealed class TestMarkdownSettings : MarkdownCommandSettingsBase
    {
    }

    private sealed class TestHtmlSettings : DocumentCommandSettingsBase
    {
    }
}
