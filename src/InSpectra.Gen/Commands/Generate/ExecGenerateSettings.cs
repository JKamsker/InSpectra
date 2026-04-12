using System.ComponentModel;
using InSpectra.Gen.Commands.Common;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Generate;

public sealed class ExecGenerateSettings : GenerateCommandSettingsBase
{
    [Description("CLI executable or script to invoke.")]
    [CommandArgument(0, "<SOURCE>")]
    public string Source { get; init; } = string.Empty;

    [Description("Additional arguments passed directly to the source executable before the export command.")]
    [CommandOption("--source-arg <ARG>")]
    public string[] SourceArguments { get; init; } = [];

    [Description("Override the arguments used to invoke the source CLI's OpenCLI export command.")]
    [CommandOption("--opencli-arg <ARG>")]
    public string[] OpenCliArguments { get; init; } = [];

    [Description("Working directory to use when invoking the source CLI.")]
    [CommandOption("--cwd <PATH>")]
    public string? WorkingDirectory { get; init; }

    [Description("Timeout in seconds for source execution.")]
    [CommandOption("--timeout <SECONDS>")]
    public int? TimeoutSeconds { get; init; }
}
