using System.Text.Json.Nodes;
using InSpectra.Gen.Commands.Render;
using InSpectra.Gen.Runtime;

namespace InSpectra.Gen.Tests;

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
    public void Hybrid_layout_requires_out_dir()
    {
        var settings = new TestMarkdownSettings();

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateMarkdownOptions(settings, "hybrid", null, null, timeoutSeconds: null, hasTimeoutSupport: false));

        Assert.Contains("`--layout hybrid` requires `--out-dir`", exception.Message);
    }

    [Fact]
    public void Hybrid_layout_rejects_out_file()
    {
        var settings = new TestMarkdownSettings();

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateMarkdownOptions(settings, "hybrid", "docs.md", null, timeoutSeconds: null, hasTimeoutSupport: false));

        Assert.Contains("`--out` is only valid with `--layout single`", exception.Message);
    }

    [Fact]
    public void Split_depth_requires_hybrid_layout()
    {
        var settings = new TestMarkdownSettings();

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateMarkdownOptions(settings, "single", "docs.md", null, timeoutSeconds: null, hasTimeoutSupport: false, splitDepth: 2));

        Assert.Contains("`--split-depth` is only valid with `--layout hybrid`", exception.Message);
    }

    [Fact]
    public void Split_depth_must_be_positive()
    {
        var settings = new TestMarkdownSettings();

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateMarkdownOptions(settings, "hybrid", null, "out", timeoutSeconds: null, hasTimeoutSupport: false, splitDepth: 0));

        Assert.Contains("`--split-depth` must be at least 1", exception.Message);
    }

    [Fact]
    public void Hybrid_layout_with_valid_split_depth_creates_markdown_render_options()
    {
        var settings = new TestMarkdownSettings();

        var options = RenderRequestFactory.CreateMarkdownOptions(settings, "hybrid", null, "out", timeoutSeconds: null, hasTimeoutSupport: false, splitDepth: 2);
        var markdownOptions = RenderRequestFactory.CreateMarkdownRenderOptions(options.Layout, 2);

        Assert.Equal(RenderLayout.Hybrid, options.Layout);
        Assert.NotNull(markdownOptions);
        Assert.Equal(2, markdownOptions!.HybridSplitDepth);
    }

    [Fact]
    public void Hybrid_layout_default_split_depth_is_one()
    {
        var settings = new TestMarkdownSettings();

        var options = RenderRequestFactory.CreateMarkdownOptions(settings, "hybrid", null, "out", timeoutSeconds: null, hasTimeoutSupport: false);
        var markdownOptions = RenderRequestFactory.CreateMarkdownRenderOptions(options.Layout, splitDepth: null);

        Assert.NotNull(markdownOptions);
        Assert.Equal(1, markdownOptions!.HybridSplitDepth);
    }

    [Fact]
    public void Non_hybrid_layout_produces_no_markdown_render_options()
    {
        Assert.Null(RenderRequestFactory.CreateMarkdownRenderOptions(RenderLayout.Single, splitDepth: null));
        Assert.Null(RenderRequestFactory.CreateMarkdownRenderOptions(RenderLayout.Tree, splitDepth: null));
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
        var settings = new TestHtmlSettings();

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateHtmlOptions(settings, null, "docs.html", null, timeoutSeconds: null, hasTimeoutSupport: false));

        Assert.Contains("`--out` is not supported", exception.Message);
    }

    [Fact]
    public void Html_output_rejects_layout()
    {
        var settings = new TestHtmlSettings
        {
            OutputDirectory = "docs",
        };

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateHtmlOptions(settings, "tree", null, settings.OutputDirectory, timeoutSeconds: null, hasTimeoutSupport: false));

        Assert.Contains("`--layout` is not supported", exception.Message);
    }

    [Fact]
    public void Html_command_settings_do_not_expose_markdown_output_flags()
    {
        var fileProperties = typeof(FileHtmlSettings).GetProperties().Select(property => property.Name).ToArray();
        var execProperties = typeof(ExecHtmlSettings).GetProperties().Select(property => property.Name).ToArray();

        Assert.DoesNotContain(nameof(MarkdownCommandSettingsBase.Layout), fileProperties);
        Assert.DoesNotContain(nameof(MarkdownCommandSettingsBase.OutputFile), fileProperties);
        Assert.Contains(nameof(HtmlCommandSettingsBase.OutputDirectory), fileProperties);

        Assert.DoesNotContain(nameof(MarkdownCommandSettingsBase.Layout), execProperties);
        Assert.DoesNotContain(nameof(MarkdownCommandSettingsBase.OutputFile), execProperties);
        Assert.Contains(nameof(HtmlCommandSettingsBase.OutputDirectory), execProperties);
    }

    [Fact]
    public void Self_doc_settings_only_expose_supported_self_doc_flags()
    {
        var properties = typeof(SelfDocSettings).GetProperties().Select(property => property.Name).ToArray();

        Assert.DoesNotContain(nameof(CommonCommandSettings.Json), properties);
        Assert.DoesNotContain(nameof(CommonCommandSettings.Output), properties);
        Assert.DoesNotContain(nameof(CommonCommandSettings.Quiet), properties);
        Assert.DoesNotContain(nameof(CommonCommandSettings.Verbose), properties);
        Assert.DoesNotContain(nameof(CommonCommandSettings.NoColor), properties);
        Assert.DoesNotContain(nameof(CommonCommandSettings.DryRun), properties);

        Assert.Contains(nameof(SelfDocHtmlCommandSettingsBase.OutputDirectory), properties);
        Assert.Contains(nameof(SelfDocCommandSettingsBase.Overwrite), properties);
        Assert.Contains(nameof(SelfDocCommandSettingsBase.IncludeHidden), properties);
        Assert.Contains(nameof(SelfDocCommandSettingsBase.IncludeMetadata), properties);
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
            var files = json["data"]!["output"]!["files"]!.AsArray();
            var file = Assert.Single(files);
            Assert.Equal("index.html", file!["relativePath"]!.GetValue<string>());
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

    private sealed class TestHtmlSettings : HtmlCommandSettingsBase
    {
    }
}
