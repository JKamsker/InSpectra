using System.ComponentModel;
using InSpectra.Gen.Commands.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Generate;

public sealed class PackageGenerateSettings : GenerateCommandSettingsBase
{
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Version))
        {
            return ValidationResult.Error("`--version` is required.");
        }

        return ValidationResult.Success();
    }

    [Description("NuGet package id for the .NET tool package to analyze.")]
    [CommandArgument(0, "<PACKAGE_ID>")]
    public string PackageId { get; init; } = string.Empty;

    [Description("Package version to install and analyze.")]
    [CommandOption("--version <VERSION>")]
    public string Version { get; init; } = string.Empty;

    [Description("Override the arguments used to invoke the installed tool's OpenCLI export command.")]
    [CommandOption("--opencli-arg <ARG>")]
    public string[] OpenCliArguments { get; init; } = [];

    [Description("Timeout in seconds for package install and command execution.")]
    [CommandOption("--timeout <SECONDS>")]
    public int? TimeoutSeconds { get; init; }
}
