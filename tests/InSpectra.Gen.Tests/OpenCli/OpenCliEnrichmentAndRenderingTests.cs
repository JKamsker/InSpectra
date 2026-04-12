using System.Text.Json.Nodes;
using InSpectra.Lib.Rendering.Pipeline.Model;
using InSpectra.Lib.Rendering.Contracts;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.OpenCli;

public class OpenCliEnrichmentAndRenderingTests
{
    private readonly OpenCliDocumentLoader _loader = new(new OpenCliSchemaProvider());
    private readonly OpenCliXmlEnricher _enricher = new();
    private readonly OpenCliNormalizer _normalizer = new();
    private readonly MarkdownRenderer _renderer = RendererFactory.CreateMarkdownRenderer();

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
        Assert.Contains("Command-line reference for `jdr`.", markdownWithoutMetadata);
        Assert.Contains("### CLI Scope", markdownWithoutMetadata);
        Assert.Contains("### Available Commands", markdownWithoutMetadata);
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
        Assert.Contains("### Available Commands", files.Single(file => file.RelativePath == "index.md").Content);
        Assert.Contains("- [auth](auth/index.md)", files.Single(file => file.RelativePath == "index.md").Content);
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
    public void Single_markdown_honors_title_override_and_prefixes_examples()
    {
        var document = new NormalizedCliDocument
        {
            Source = new OpenCliDocument
            {
                OpenCliVersion = "0.1-draft",
                Info = new OpenCliInfo
                {
                    Title = "jf",
                    Version = "1.0.0",
                },
                Examples = ["auth login --username demo"],
            },
            RootArguments = [],
            RootOptions = [],
            Commands =
            [
                new NormalizedCommand
                {
                    Path = "auth",
                    Command = new OpenCliCommand
                    {
                        Name = "auth",
                        Description = "Authentication commands.",
                        Examples = ["auth whoami"],
                    },
                    Arguments = [],
                    DeclaredOptions = [],
                    InheritedOptions = [],
                    Commands = [],
                },
            ],
        };

        var markdown = _renderer.RenderSingle(
            document,
            includeMetadata: false,
            new MarkdownRenderOptions(HybridSplitDepth: 1, Title: "JellyfinCli", CommandPrefix: "jf"));

        Assert.Contains("# JellyfinCli", markdown);
        Assert.Contains("Command-line reference for `JellyfinCli`.", markdown);
        Assert.Contains("- `jf auth login --username demo`", markdown);
        Assert.Contains("- `jf auth whoami`", markdown);
        Assert.DoesNotContain("# jf", markdown);
    }

    [Fact]
    public void Hybrid_markdown_uses_title_override_in_readme_and_prefixes_group_examples()
    {
        var document = new NormalizedCliDocument
        {
            Source = new OpenCliDocument
            {
                OpenCliVersion = "0.1-draft",
                Info = new OpenCliInfo
                {
                    Title = "jf",
                    Version = "1.0.0",
                },
            },
            RootArguments = [],
            RootOptions = [],
            Commands =
            [
                new NormalizedCommand
                {
                    Path = "library",
                    Command = new OpenCliCommand
                    {
                        Name = "library",
                        Description = "Library commands.",
                        Examples = ["library list"],
                    },
                    Arguments = [],
                    DeclaredOptions = [],
                    InheritedOptions = [],
                    Commands =
                    [
                        new NormalizedCommand
                        {
                            Path = "library refresh",
                            Command = new OpenCliCommand
                            {
                                Name = "refresh",
                                Description = "Refresh library metadata.",
                                Examples = ["library refresh --all"],
                            },
                            Arguments = [],
                            DeclaredOptions = [],
                            InheritedOptions = [],
                            Commands = [],
                        },
                    ],
                },
            ],
        };

        var files = _renderer.RenderHybrid(
            document,
            includeMetadata: false,
            splitDepth: 1,
            new MarkdownRenderOptions(HybridSplitDepth: 1, Title: "JellyfinCli", CommandPrefix: "jf"));
        var readme = files.Single(file => file.RelativePath == "README.md").Content;
        var group = files.Single(file => file.RelativePath == "library/index.md").Content;

        Assert.Contains("# JellyfinCli", readme);
        Assert.Contains("Command-line reference for `JellyfinCli`.", readme);
        Assert.Contains("- `jf library list`", group);
        Assert.Contains("- `jf library refresh --all`", group);
    }

}
