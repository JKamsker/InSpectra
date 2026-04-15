using InSpectra.Lib;
using InSpectra.Gen.Cli;
using InSpectra.Gen.Commands.Generate;
using InSpectra.Gen.Commands.Render;
using InSpectra.Lib.UseCases.Generate.Requests;
using InSpectra.Lib.Rendering.Contracts;
using InSpectra.Gen.Commands.Common;

namespace InSpectra.Gen.Tests.Output;

public class RenderRequestContractTests
{
    [Fact]
    public void Json_single_output_requires_out_file()
    {
        var settings = new TestMarkdownSettings
        {
            Json = true,
        };

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateMarkdownOptions(settings, ResolvedOutputMode.Json, "single", null, null, timeoutSeconds: null, hasTimeoutSupport: false));
        Assert.Contains("requires `--out`", exception.Message);
    }

    [Fact]
    public void Hybrid_layout_requires_out_dir()
    {
        var settings = new TestMarkdownSettings();

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateMarkdownOptions(settings, ResolvedOutputMode.Human, "hybrid", null, null, timeoutSeconds: null, hasTimeoutSupport: false));
        Assert.Contains("`--layout hybrid` requires `--out-dir`", exception.Message);
    }

    [Fact]
    public void Hybrid_layout_rejects_out_file()
    {
        var settings = new TestMarkdownSettings();

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateMarkdownOptions(settings, ResolvedOutputMode.Human, "hybrid", "docs.md", null, timeoutSeconds: null, hasTimeoutSupport: false));
        Assert.Contains("`--out` is only valid with `--layout single`", exception.Message);
    }

    [Fact]
    public void Split_depth_requires_hybrid_layout()
    {
        var settings = new TestMarkdownSettings();

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateMarkdownOptions(settings, ResolvedOutputMode.Human, "single", "docs.md", null, timeoutSeconds: null, hasTimeoutSupport: false, splitDepth: 2));
        Assert.Contains("`--split-depth` is only valid with `--layout hybrid`", exception.Message);
    }

    [Fact]
    public void Split_depth_must_be_positive()
    {
        var settings = new TestMarkdownSettings();

        var exception = Assert.Throws<CliUsageException>(() =>
            RenderRequestFactory.CreateMarkdownOptions(settings, ResolvedOutputMode.Human, "hybrid", null, "out", timeoutSeconds: null, hasTimeoutSupport: false, splitDepth: 0));
        Assert.Contains("`--split-depth` must be at least 1", exception.Message);
    }

    [Fact]
    public void Hybrid_layout_with_valid_split_depth_creates_markdown_render_options()
    {
        var settings = new TestMarkdownSettings();

        var options = RenderRequestFactory.CreateMarkdownOptions(settings, ResolvedOutputMode.Human, "hybrid", null, "out", timeoutSeconds: null, hasTimeoutSupport: false, splitDepth: 2);
        var markdownOptions = RenderRequestFactory.CreateMarkdownRenderOptions(settings, options.Layout, 2);

        Assert.Equal(RenderLayout.Hybrid, options.Layout);
        Assert.NotNull(markdownOptions);
        Assert.Equal(2, markdownOptions!.HybridSplitDepth);
    }

    [Fact]
    public void Hybrid_layout_default_split_depth_is_one()
    {
        var settings = new TestMarkdownSettings();

        var options = RenderRequestFactory.CreateMarkdownOptions(settings, ResolvedOutputMode.Human, "hybrid", null, "out", timeoutSeconds: null, hasTimeoutSupport: false);
        var markdownOptions = RenderRequestFactory.CreateMarkdownRenderOptions(settings, options.Layout, splitDepth: null);

        Assert.NotNull(markdownOptions);
        Assert.Equal(1, markdownOptions!.HybridSplitDepth);
    }

    [Fact]
    public void Non_hybrid_layout_produces_no_markdown_render_options()
    {
        var settings = new TestMarkdownSettings();

        Assert.Null(RenderRequestFactory.CreateMarkdownRenderOptions(settings, RenderLayout.Single, splitDepth: null));
        Assert.Null(RenderRequestFactory.CreateMarkdownRenderOptions(settings, RenderLayout.Tree, splitDepth: null));
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
    public void Html_output_defaults_to_single_file_bundle()
    {
        var settings = new TestHtmlSettings
        {
            OutputDirectory = "docs",
        };

        var options = RenderRequestFactory.CreateHtmlOptions(settings, null, null, settings.OutputDirectory, timeoutSeconds: null, hasTimeoutSupport: false);

        Assert.True(options.SingleFile);
        Assert.Equal(2, options.CompressLevel);
    }

    [Fact]
    public void Html_single_file_defaults_to_self_extracting_bundle()
    {
        var settings = new TestHtmlSettings
        {
            OutputDirectory = "docs",
            SingleFile = true,
        };

        var options = RenderRequestFactory.CreateHtmlOptions(settings, null, null, settings.OutputDirectory, timeoutSeconds: null, hasTimeoutSupport: false);

        Assert.True(options.SingleFile);
        Assert.Equal(2, options.CompressLevel);
    }

    [Fact]
    public void Html_theme_options_treat_blank_accent_as_missing()
    {
        var settings = new TestHtmlSettings
        {
            Accent = "   ",
            AccentDark = "#112233",
        };

        var exception = Assert.Throws<CliUsageException>(() => RenderRequestFactory.CreateHtmlThemeOptions(settings));

        Assert.Contains("`--accent-dark` requires `--accent`", exception.Message);
    }

    [Fact]
    public void Html_command_settings_do_not_expose_markdown_output_flags()
    {
        var fileProperties = typeof(FileHtmlSettings).GetProperties().Select(property => property.Name).ToArray();

        Assert.DoesNotContain(nameof(MarkdownCommandSettingsBase.Layout), fileProperties);
        Assert.DoesNotContain(nameof(MarkdownCommandSettingsBase.OutputFile), fileProperties);
        Assert.Contains(nameof(HtmlCommandSettingsBase.OutputDirectory), fileProperties);
    }

    [Theory]
    [InlineData("native", OpenCliMode.Native)]
    [InlineData("auto", OpenCliMode.Auto)]
    [InlineData("help", OpenCliMode.Help)]
    [InlineData("clifx", OpenCliMode.CliFx)]
    [InlineData("static", OpenCliMode.Static)]
    [InlineData("hook", OpenCliMode.Hook)]
    public void OpenCli_mode_parsing_supports_all_public_values(string value, OpenCliMode expected)
    {
        var mode = CommandValueResolver.ResolveOpenCliMode(value, OpenCliMode.Native);
        Assert.Equal(expected, mode);
    }

    [Fact]
    public void Generate_settings_expose_xml_enrichment_flags()
    {
        var execProperties = typeof(ExecGenerateSettings).GetProperties().Select(property => property.Name).ToArray();
        var dotnetProperties = typeof(DotnetGenerateSettings).GetProperties().Select(property => property.Name).ToArray();
        var packageProperties = typeof(PackageGenerateSettings).GetProperties().Select(property => property.Name).ToArray();

        Assert.Contains(nameof(GenerateCommandSettingsBase.WithXmlDoc), execProperties);
        Assert.Contains(nameof(GenerateCommandSettingsBase.XmlDocArguments), execProperties);
        Assert.Contains(nameof(GenerateCommandSettingsBase.WithXmlDoc), dotnetProperties);
        Assert.Contains(nameof(GenerateCommandSettingsBase.XmlDocArguments), dotnetProperties);
        Assert.Contains(nameof(GenerateCommandSettingsBase.WithXmlDoc), packageProperties);
        Assert.Contains(nameof(GenerateCommandSettingsBase.XmlDocArguments), packageProperties);
    }

    [Fact]
    public void Generate_settings_do_not_expose_render_only_flags()
    {
        var execProperties = typeof(ExecGenerateSettings).GetProperties().Select(property => property.Name).ToArray();

        Assert.DoesNotContain(nameof(CommonCommandSettings.DryRun), execProperties);
        Assert.DoesNotContain(nameof(CommonCommandSettings.IncludeHidden), execProperties);
        Assert.DoesNotContain(nameof(CommonCommandSettings.IncludeMetadata), execProperties);
        Assert.DoesNotContain(nameof(CommonCommandSettings.Quiet), execProperties);
        Assert.DoesNotContain(nameof(CommonCommandSettings.NoColor), execProperties);
    }

    [Fact]
    public void Render_execution_options_do_not_expose_shell_only_flags()
    {
        var renderProperties = typeof(RenderExecutionOptions).GetProperties().Select(property => property.Name).ToArray();

        Assert.DoesNotContain("OutputMode", renderProperties);
        Assert.DoesNotContain("Quiet", renderProperties);
        Assert.DoesNotContain("Verbose", renderProperties);
        Assert.DoesNotContain("NoColor", renderProperties);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Explicit_timeout_must_be_positive(int timeoutSeconds)
    {
        var exception = Assert.Throws<CliUsageException>(() =>
            CommandValueResolver.ResolveTimeoutSeconds(timeoutSeconds));

        Assert.Contains("`--timeout` must be a positive integer.", exception.Message);
    }

    [Fact]
    public void Explicit_output_overrides_invalid_environment_output()
    {
        const string environmentVariableName = "INSPECTRA_GEN_OUTPUT";
        var originalValue = Environment.GetEnvironmentVariable(environmentVariableName);
        try
        {
            Environment.SetEnvironmentVariable(environmentVariableName, "broken");

            var mode = CommandValueResolver.ResolveOutputMode(json: false, output: "human");

            Assert.Equal(ResolvedOutputMode.Human, mode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariableName, originalValue);
        }
    }

    [Fact]
    public void File_render_settings_do_not_expose_acquisition_flags()
    {
        var markdownProperties = typeof(FileRenderSettings).GetProperties().Select(property => property.Name).ToArray();
        var htmlProperties = typeof(FileHtmlSettings).GetProperties().Select(property => property.Name).ToArray();

        Assert.DoesNotContain(nameof(GenerateCommandSettingsBase.OpenCliMode), markdownProperties);
        Assert.DoesNotContain(nameof(GenerateCommandSettingsBase.CommandName), markdownProperties);
        Assert.DoesNotContain(nameof(GenerateCommandSettingsBase.CliFramework), markdownProperties);
        Assert.DoesNotContain(nameof(GenerateCommandSettingsBase.OpenCliMode), htmlProperties);
        Assert.DoesNotContain(nameof(GenerateCommandSettingsBase.CommandName), htmlProperties);
        Assert.DoesNotContain(nameof(GenerateCommandSettingsBase.CliFramework), htmlProperties);
    }

    private sealed class TestMarkdownSettings : MarkdownCommandSettingsBase
    {
    }

    private sealed class TestHtmlSettings : HtmlCommandSettingsBase
    {
    }
}
