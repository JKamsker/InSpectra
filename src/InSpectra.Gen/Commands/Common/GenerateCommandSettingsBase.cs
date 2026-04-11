using System.ComponentModel;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Common;

public abstract class GenerateCommandSettingsBase : OutputCommandSettingsBase
{
    [Description("OpenCLI acquisition mode: native, auto, help, clifx, static, or hook.")]
    [CommandOption("--opencli-mode <MODE>")]
    public string? OpenCliMode { get; init; }

    [Description("Override the root command name used for generated OpenCLI documents.")]
    [CommandOption("--command <NAME>")]
    public string? CommandName { get; init; }

    [Description("Hint or override the detected CLI framework for non-native analysis.")]
    [CommandOption("--cli-framework <NAME>")]
    public string? CliFramework { get; init; }

    [Description("Also run the source CLI's XML documentation export command and enrich the generated OpenCLI document with its output.")]
    [CommandOption("--with-xmldoc")]
    public bool WithXmlDoc { get; init; }

    [Description("Override the arguments used to invoke the source CLI's XML documentation export command.")]
    [CommandOption("--xmldoc-arg <ARG>")]
    public string[] XmlDocArguments { get; init; } = [];

    [Description("Write the generated OpenCLI JSON to this file instead of stdout.")]
    [CommandOption("--out <FILE>")]
    public string? OutputFile { get; init; }

    [Description("Allow an existing OpenCLI output file to be replaced.")]
    [CommandOption("--overwrite")]
    public bool Overwrite { get; init; }

    [Description("Write crawl.json when the selected acquisition mode produces crawl data.")]
    [CommandOption("--crawl-out <PATH>")]
    public string? CrawlOutputPath { get; init; }
}
