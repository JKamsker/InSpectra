using System.ComponentModel;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Common;

/// <summary>
/// Shared Markdown-output settings for file render commands.
/// </summary>
public abstract class MarkdownCommandSettingsBase : CommonCommandSettings
{
    [Description("Markdown layout mode. Supported values are single, tree, and hybrid.")]
    [CommandOption("--layout <LAYOUT>")]
    public string? Layout { get; init; }

    [Description("Single Markdown file to write when using the single layout.")]
    [CommandOption("--out <FILE>")]
    public string? OutputFile { get; init; }

    [Description("Output directory to write when using the tree or hybrid layout.")]
    [CommandOption("--out-dir <DIR>")]
    public string? OutputDirectory { get; init; }

    [Description("Depth at which hybrid layout emits one file per command group (defaults to 1).")]
    [CommandOption("--split-depth <DEPTH>")]
    public int? SplitDepth { get; init; }

    [Description("Override the CLI title shown in Markdown headings and overview text.")]
    [CommandOption("--title <TEXT>")]
    public string? Title { get; init; }

    [Description("Override the CLI command prefix used in rendered Markdown examples.")]
    [CommandOption("--command-prefix <TEXT>")]
    public string? CommandPrefix { get; init; }
}
