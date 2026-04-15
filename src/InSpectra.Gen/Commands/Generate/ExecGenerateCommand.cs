using InSpectra.Gen.Commands.Common;
using InSpectra.Gen.Cli.Output;
using InSpectra.Lib.UseCases.Generate.Requests;
using InSpectra.Lib.UseCases.Generate;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Generate;

public sealed class ExecGenerateCommand(IOpenCliGenerationService generationService) : AsyncCommand<ExecGenerateSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, ExecGenerateSettings settings, CancellationToken cancellationToken)
    {
        var outputMode = CommandValueResolver.ResolveOutputMode(settings.Json, settings.Output);
        var request = new ExecAcquisitionRequest(
            settings.Source,
            settings.SourceArguments,
            CommandValueResolver.ResolveWorkingDirectory(settings.WorkingDirectory),
            new AcquisitionOptions(
                CommandValueResolver.ResolveOpenCliMode(settings.OpenCliMode, OpenCliMode.Auto),
                settings.CommandName,
                settings.CliFramework,
                settings.OpenCliArguments.Length > 0 ? settings.OpenCliArguments : OpenCliExportCommandDefaults.OpenCliArguments,
                settings.WithXmlDoc,
                settings.XmlDocArguments.Length > 0 ? settings.XmlDocArguments : OpenCliExportCommandDefaults.XmlDocArguments,
                CommandValueResolver.ResolveTimeoutSeconds(settings.TimeoutSeconds),
                new OpenCliArtifactOptions(null, settings.CrawlOutputPath, settings.Overwrite)));

        return GenerateOutputHandler.ExecuteAsync(
            outputMode,
            settings.Verbose,
            () => generationService.GenerateFromExecAsync(request, settings.OutputFile, settings.Overwrite, cancellationToken));
    }
}
