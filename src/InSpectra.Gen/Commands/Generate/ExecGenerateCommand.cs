using InSpectra.Gen.Runtime;
using InSpectra.Gen.Runtime.Acquisition;
using InSpectra.Gen.Runtime.Output;
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
            new OpenCliArtifactOptions(null, settings.CrawlOutputPath, settings.Overwrite));

        return GenerateOutputHandler.ExecuteAsync(
            outputMode,
            settings.Verbose,
            () => generationService.GenerateFromExecAsync(request, settings.OutputFile, settings.Overwrite, cancellationToken));
    }
}
