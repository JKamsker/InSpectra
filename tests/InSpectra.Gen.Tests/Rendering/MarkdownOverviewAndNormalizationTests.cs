using System.Text.Json.Nodes;
using InSpectra.Gen.Rendering.Pipeline.Model;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.Rendering;

public class MarkdownOverviewAndNormalizationTests
{
    private readonly OpenCliNormalizer _normalizer = new();
    private readonly MarkdownRenderer _renderer = RendererFactory.CreateMarkdownRenderer();

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
    public void Single_markdown_renders_complex_metadata_as_indented_code_blocks()
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
                Metadata =
                [
                    new OpenCliMetadata
                    {
                        Name = "Schema",
                        Value = new JsonObject
                        {
                            ["kind"] = "demo",
                            ["enabled"] = true,
                        },
                    },
                ],
            },
            RootArguments = [],
            RootOptions = [],
            Commands = [],
        };

        var markdown = _renderer.RenderSingle(document, includeMetadata: true);

        Assert.Contains("- `Schema`:", markdown);
        Assert.Contains("  ```json", markdown);
        Assert.Contains("  {", markdown);
        Assert.DoesNotContain("- `Schema`: ```json", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Overview_summary_can_detect_jellyfin_shape()
    {
        var formatter = new OverviewFormatter();
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
                    Path = "users",
                    Command = new OpenCliCommand
                    {
                        Name = "users",
                        Description = "Manage Jellyfin users.",
                    },
                    Arguments = [],
                    DeclaredOptions = [],
                    InheritedOptions = [],
                    Commands = [],
                },
                new NormalizedCommand
                {
                    Path = "server",
                    Command = new OpenCliCommand
                    {
                        Name = "server",
                        Description = "Health, logs, config, restart, and shutdown.",
                    },
                    Arguments = [],
                    DeclaredOptions = [],
                    InheritedOptions = [],
                    Commands = [],
                },
            ],
        };

        var summary = formatter.BuildSummary(document);

        Assert.Equal(
            "Manage your Jellyfin server from the command line. Available command areas include server administration and users.",
            summary);
    }

    [Fact]
    public void Hidden_default_command_renders_as_root_options()
    {
        var document = new OpenCliDocument
        {
            OpenCliVersion = "0.1-draft",
            Info = new OpenCliInfo
            {
                Title = "nupu",
                Version = "1.0.50",
            },
            Commands =
            [
                new OpenCliCommand
                {
                    Name = "__default_command",
                    Hidden = true,
                    Options =
                    [
                        new OpenCliOption
                        {
                            Name = "--directory",
                            Aliases = ["-d"],
                            Description = "A root directory to search.",
                        },
                    ],
                },
            ],
        };

        var normalized = _normalizer.Normalize(document, includeHidden: false);
        var markdown = _renderer.RenderSingle(normalized, includeMetadata: false);

        Assert.Empty(normalized.Commands);
        Assert.Single(normalized.RootOptions);
        Assert.Equal("--directory", normalized.RootOptions[0].Name);
        Assert.Contains("## Root Options", markdown);
        Assert.Contains("--directory", markdown);
        Assert.DoesNotContain("__default_command", markdown);
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
