using System.ComponentModel;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Runtime.Settings;

public abstract class OutputCommandSettingsBase : CommandSettings
{
    [Description("Emit the stable machine-readable JSON envelope instead of human output.")]
    [CommandOption("--json")]
    public bool Json { get; init; }

    [Description("Override the output mode. Supported values are human and json.")]
    [CommandOption("--output <MODE>")]
    public string? Output { get; init; }

    [Description("Increase diagnostic detail in command failures.")]
    [CommandOption("--verbose")]
    public bool Verbose { get; init; }
}
