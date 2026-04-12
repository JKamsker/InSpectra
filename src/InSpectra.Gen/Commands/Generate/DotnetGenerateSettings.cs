using System.ComponentModel;
using InSpectra.Gen.Commands.Common;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Generate;

public sealed class DotnetGenerateSettings : GenerateCommandSettingsBase
{
    [Description("Path to a .NET project file (.csproj/.fsproj/.vbproj) or a directory containing one.")]
    [CommandArgument(0, "<PROJECT>")]
    public string Project { get; init; } = string.Empty;

    [Description("Build configuration passed to dotnet run/build (e.g. Release).")]
    [CommandOption("-c|--configuration <CONFIG>")]
    public string? Configuration { get; init; }

    [Description("Target framework moniker passed to dotnet run/build (e.g. net10.0).")]
    [CommandOption("-f|--framework <TFM>")]
    public string? Framework { get; init; }

    [Description("Launch profile to use for dotnet run native mode.")]
    [CommandOption("--launch-profile <NAME>")]
    public string? LaunchProfile { get; init; }

    [Description("Skip the implicit build step for dotnet run/build.")]
    [CommandOption("--no-build")]
    public bool NoBuild { get; init; }

    [Description("Skip the implicit restore step for dotnet run/build.")]
    [CommandOption("--no-restore")]
    public bool NoRestore { get; init; }

    [Description("Override the arguments used to invoke the project's OpenCLI export command.")]
    [CommandOption("--opencli-arg <ARG>")]
    public string[] OpenCliArguments { get; init; } = [];

    [Description("Working directory used when invoking dotnet.")]
    [CommandOption("--cwd <PATH>")]
    public string? WorkingDirectory { get; init; }

    [Description("Timeout in seconds for dotnet execution.")]
    [CommandOption("--timeout <SECONDS>")]
    public int? TimeoutSeconds { get; init; }
}
