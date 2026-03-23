using Spectre.Console.Cli;

namespace OpenCli.Renderer.Runtime;

public abstract class CommonCommandSettings : CommandSettings
{
    [CommandOption("--json")]
    public bool Json { get; init; }

    [CommandOption("--output <MODE>")]
    public string? Output { get; init; }

    [CommandOption("-q|--quiet")]
    public bool Quiet { get; init; }

    [CommandOption("--verbose")]
    public bool Verbose { get; init; }

    [CommandOption("--no-color")]
    public bool NoColor { get; init; }

    [CommandOption("--dry-run")]
    public bool DryRun { get; init; }

    [CommandOption("--overwrite")]
    public bool Overwrite { get; init; }

    [CommandOption("--include-hidden")]
    public bool IncludeHidden { get; init; }

    [CommandOption("--include-metadata")]
    public bool IncludeMetadata { get; init; }
}

public abstract class MarkdownCommandSettingsBase : CommonCommandSettings
{
    [CommandOption("--layout <LAYOUT>")]
    public string? Layout { get; init; }

    [CommandOption("--out <FILE>")]
    public string? OutputFile { get; init; }

    [CommandOption("--out-dir <DIR>")]
    public string? OutputDirectory { get; init; }
}
