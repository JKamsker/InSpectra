using InSpectra.Gen.Commands.Render;
using InSpectra.Gen.UseCases.Generate.Requests;
using InSpectra.Gen.Output;
using InSpectra.Gen.UseCases.Generate;
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
            RenderRequestFactory.ResolveWorkingDirectory(settings.WorkingDirectory),
            new AcquisitionOptions(
                RenderRequestFactory.ResolveOpenCliMode(settings.OpenCliMode, OpenCliMode.Auto),
                settings.CommandName,
                settings.CliFramework,
                settings.OpenCliArguments.Length > 0 ? settings.OpenCliArguments : ["cli", "opencli"],
                settings.WithXmlDoc,
                settings.XmlDocArguments.Length > 0 ? settings.XmlDocArguments : ["cli", "xmldoc"],
                RenderRequestFactory.ResolveTimeoutSeconds(settings.TimeoutSeconds),
                new OpenCliArtifactOptions(null, settings.CrawlOutputPath, settings.Overwrite)));

        return GenerateOutputHandler.ExecuteAsync(
            outputMode,
            settings.Verbose,
            () => generationService.GenerateFromExecAsync(request, settings.OutputFile, settings.Overwrite, cancellationToken));
    }
}
