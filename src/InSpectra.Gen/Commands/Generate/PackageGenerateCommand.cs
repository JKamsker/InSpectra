using InSpectra.Gen.Commands.Common;
using InSpectra.Gen.Cli.Output;
using InSpectra.Lib.UseCases.Generate.Requests;
using InSpectra.Lib.UseCases.Generate;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Generate;

public sealed class PackageGenerateCommand(IOpenCliGenerationService generationService) : AsyncCommand<PackageGenerateSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, PackageGenerateSettings settings, CancellationToken cancellationToken)
    {
        var outputMode = CommandValueResolver.ResolveOutputMode(settings.Json, settings.Output);
        var request = new PackageAcquisitionRequest(
            settings.PackageId,
            settings.Version,
            new AcquisitionOptions(
                CommandValueResolver.ResolveOpenCliMode(settings.OpenCliMode, OpenCliMode.Auto),
                settings.CommandName,
                settings.CliFramework,
                settings.OpenCliArguments.Length > 0 ? settings.OpenCliArguments : OpenCliExportCommandDefaults.OpenCliArguments,
                settings.WithXmlDoc,
                settings.XmlDocArguments.Length > 0 ? settings.XmlDocArguments : OpenCliExportCommandDefaults.XmlDocArguments,
                CommandValueResolver.ResolveTimeoutSeconds(settings.TimeoutSeconds, defaultSeconds: 120),
                new OpenCliArtifactOptions(null, settings.CrawlOutputPath, settings.Overwrite)));

        return GenerateOutputHandler.ExecuteAsync(
            outputMode,
            settings.Verbose,
            () => generationService.GenerateFromPackageAsync(request, settings.OutputFile, settings.Overwrite, cancellationToken));
    }
}
