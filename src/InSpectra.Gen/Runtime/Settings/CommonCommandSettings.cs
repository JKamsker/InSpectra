using System.ComponentModel;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Runtime.Settings;

/// <summary>
/// Shared CLI flags for commands that render documentation.
/// </summary>
public abstract class CommonCommandSettings : OutputCommandSettingsBase
{
    [Description("Suppress non-essential console output.")]
    [CommandOption("-q|--quiet")]
    public bool Quiet { get; init; }

    [Description("Disable ANSI color sequences in human-readable console output.")]
    [CommandOption("--no-color")]
    public bool NoColor { get; init; }

    [Description("Preview the resolved render plan without writing files.")]
    [CommandOption("--dry-run")]
    public bool DryRun { get; init; }

    [Description("Allow existing output files or directories to be replaced.")]
    [CommandOption("--overwrite")]
    public bool Overwrite { get; init; }

    [Description("Include commands and options marked hidden by the source CLI.")]
    [CommandOption("--include-hidden")]
    public bool IncludeHidden { get; init; }

    [Description("Include metadata sections in the rendered Markdown or HTML output.")]
    [CommandOption("--include-metadata")]
    public bool IncludeMetadata { get; init; }
}
