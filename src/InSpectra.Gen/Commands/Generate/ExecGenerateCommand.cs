using System.ComponentModel;
using InSpectra.Gen.Runtime;
using InSpectra.Gen.Services;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Generate;

public sealed class ExecGenerateCommand(IOpenCliGenerationService generationService) : AsyncCommand<ExecGenerateSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, ExecGenerateSettings settings, CancellationToken cancellationToken)
    {
        var outputMode = RenderRequestFactory.ResolveOutputMode(settings);
        var request = new ExecAcquisitionRequest(
            settings.Source,
            settings.SourceArguments,
            RenderRequestFactory.ResolveOpenCliMode(settings.OpenCliMode, OpenCliMode.Auto),
            settings.CommandName,
            settings.CliFramework,
            settings.OpenCliArguments.Length > 0 ? settings.OpenCliArguments : ["cli", "opencli"],
            settings.WithXmlDoc,
            settings.XmlDocArguments.Length > 0 ? settings.XmlDocArguments : ["cli", "xmldoc"],
            RenderRequestFactory.ResolveWorkingDirectory(settings.WorkingDirectory),
            RenderRequestFactory.ResolveTimeoutSeconds(settings.TimeoutSeconds),
            new OpenCliArtifactOptions(null, settings.CrawlOutputPath));

        return GenerateOutputHandler.ExecuteAsync(
            outputMode,
            settings.Verbose,
            () => generationService.GenerateFromExecAsync(request, settings.OutputFile, cancellationToken));
    }
}

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
