using System.Text.Json.Nodes;
using OpenCli.Renderer.Models;
using OpenCli.Renderer.Services;
using OpenCli.Renderer.Tests.TestSupport;

namespace OpenCli.Renderer.Tests;

public class OpenCliEnrichmentAndRenderingTests
{
    private readonly OpenCliDocumentLoader _loader = new(new OpenCliSchemaProvider());
    private readonly OpenCliXmlEnricher _enricher = new();
    private readonly OpenCliNormalizer _normalizer = new();
    private readonly MarkdownRenderer _renderer = RendererFactory.CreateMarkdownRenderer();
    private readonly HtmlRenderer _htmlRenderer = RendererFactory.CreateHtmlRenderer();

    [Fact]
    public async Task Xml_enrichment_restores_missing_command_descriptions()
    {
        var document = await _loader.LoadFromFileAsync(FixturePaths.OpenCliJson, CancellationToken.None);
        var authLogin = document.Commands
            .Single(command => command.Name == "auth")
            .Commands
            .Single(command => command.Name == "login");

        authLogin.Description = null;

        var enrichment = await _enricher.EnrichFromFileAsync(document, FixturePaths.XmlDoc, CancellationToken.None);

        Assert.Equal("Store encrypted auth material for a profile.", authLogin.Description);
        Assert.True(enrichment.MatchedCommandCount > 0);
    }

    [Fact]
    public async Task Single_markdown_omits_metadata_by_default_and_can_include_it()
    {
        var document = await _loader.LoadFromFileAsync(FixturePaths.OpenCliJson, CancellationToken.None);
        var normalized = _normalizer.Normalize(document, includeHidden: false);

        var markdownWithoutMetadata = _renderer.RenderSingle(normalized, includeMetadata: false);
        var markdownWithMetadata = _renderer.RenderSingle(normalized, includeMetadata: true);

        Assert.Contains("# jdr", markdownWithoutMetadata);
        Assert.Contains("## Commands", markdownWithoutMetadata);
        Assert.Contains("`auth login`", markdownWithoutMetadata);
        Assert.DoesNotContain("Metadata Appendix", markdownWithoutMetadata);
        Assert.Contains("Metadata Appendix", markdownWithMetadata);
        Assert.Contains("ClrType", markdownWithMetadata);
    }

    [Fact]
    public async Task Tree_markdown_creates_expected_command_pages()
    {
        var document = await _loader.LoadFromFileAsync(FixturePaths.OpenCliJson, CancellationToken.None);
        var normalized = _normalizer.Normalize(document, includeHidden: false);

        var files = _renderer.RenderTree(normalized, includeMetadata: false);

        Assert.Contains(files, file => file.RelativePath == "index.md");
        Assert.Contains(files, file => file.RelativePath == "auth/index.md");
        Assert.Contains(files, file => file.RelativePath == "auth/login.md");
        Assert.Contains("Store encrypted auth material for a profile.", files.Single(file => file.RelativePath == "auth/login.md").Content);
    }

    [Fact]
    public async Task Tree_markdown_includes_option_argument_details_and_metadata()
    {
        var document = await _loader.LoadFromFileAsync(FixturePaths.OpenCliJson, CancellationToken.None);
        var normalized = _normalizer.Normalize(document, includeHidden: false);

        var files = _renderer.RenderTree(normalized, includeMetadata: true);
        var markdown = files.Single(file => file.RelativePath == "auth/login.md").Content;

        Assert.Contains("| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |", markdown);
        Assert.Contains("EMAIL · required · arity 1", markdown);
        Assert.Contains("Metadata Appendix", markdown);
        Assert.Contains("Argument `EMAIL`", markdown);
        Assert.Contains("System.String", markdown);
    }

    [Fact]
    public void Single_markdown_includes_root_parameter_metadata()
    {
        var document = new NormalizedCliDocument
        {
            Source = new OpenCliDocument
            {
                OpenCliVersion = "0.1-draft",
                Info = new OpenCliInfo
                {
                    Title = "demo",
                    Version = "1.0.0",
                },
                Metadata = [CreateMetadata("RootKind", "demo")],
            },
            RootArguments =
            [
                new OpenCliArgument
                {
                    Name = "TARGET",
                    Required = true,
                    Description = "Target to inspect.",
                    Metadata = [CreateMetadata("ClrType", "System.String")],
                },
            ],
            RootOptions =
            [
                new OpenCliOption
                {
                    Name = "--profile",
                    Description = "Profile override.",
                    Metadata = [CreateMetadata("Settings", "Demo.Profile")],
                    Arguments =
                    [
                        new OpenCliArgument
                        {
                            Name = "NAME",
                            Required = true,
                            Description = "Profile name.",
                            Metadata = [CreateMetadata("ClrType", "System.String")],
                        },
                    ],
                },
            ],
            Commands = [],
        };

        var markdown = _renderer.RenderSingle(document, includeMetadata: true);

        Assert.Contains("## Metadata Appendix", markdown);
        Assert.Contains("### Root Arguments", markdown);
        Assert.Contains("### Root Options", markdown);
        Assert.Contains("Profile name.", markdown);
        Assert.Contains("Argument `NAME`", markdown);
        Assert.Contains("`Settings`: `Demo.Profile`", markdown);
        Assert.Contains("`ClrType`: `System.String`", markdown);
    }

    [Fact]
    public async Task Single_html_uses_viewer_inspired_shell()
    {
        var document = await _loader.LoadFromFileAsync(FixturePaths.OpenCliJson, CancellationToken.None);
        var normalized = _normalizer.Normalize(document, includeHidden: false);

        var html = _htmlRenderer.RenderSingle(normalized, includeMetadata: true);

        Assert.Contains("<aside class=\"sidebar\">", html);
        Assert.Contains("Filter commands", html);
        Assert.Contains("command-card", html);
        Assert.Contains("option-card", html);
        Assert.Contains("Metadata appendix", html);
    }

    [Fact]
    public async Task Tree_html_creates_expected_command_pages()
    {
        var document = await _loader.LoadFromFileAsync(FixturePaths.OpenCliJson, CancellationToken.None);
        var normalized = _normalizer.Normalize(document, includeHidden: false);

        var files = _htmlRenderer.RenderTree(normalized, includeMetadata: false);

        Assert.Contains(files, file => file.RelativePath == "index.html");
        Assert.Contains(files, file => file.RelativePath == "auth/index.html");
        Assert.Contains(files, file => file.RelativePath == "auth/login.html");
        Assert.Contains("Store encrypted auth material for a profile.", files.Single(file => file.RelativePath == "auth/login.html").Content);
        Assert.Contains("Search command tree", files.Single(file => file.RelativePath == "auth/login.html").Content);
    }

    private static OpenCliMetadata CreateMetadata(string name, string value)
    {
        return new OpenCliMetadata
        {
            Name = name,
            Value = JsonValue.Create(value),
        };
    }
}
