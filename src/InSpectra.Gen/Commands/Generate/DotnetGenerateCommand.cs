using InSpectra.Gen.Commands.Common;
using InSpectra.Gen.Cli.Output;
using InSpectra.Lib.UseCases.Generate.Requests;
using InSpectra.Lib.UseCases.Generate;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Generate;

public sealed class DotnetGenerateCommand(IOpenCliGenerationService generationService) : AsyncCommand<DotnetGenerateSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, DotnetGenerateSettings settings, CancellationToken cancellationToken)
    {
        var outputMode = CommandValueResolver.ResolveOutputMode(settings.Json, settings.Output);
        var workingDirectory = CommandValueResolver.ResolveWorkingDirectory(settings.WorkingDirectory);
        var request = new DotnetAcquisitionRequest(
            new DotnetBuildSettings(
                DotnetProjectResolver.Resolve(settings.Project, workingDirectory),
                settings.Configuration,
                settings.Framework,
                settings.LaunchProfile,
                settings.NoBuild,
                settings.NoRestore),
            workingDirectory,
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
            () => generationService.GenerateFromDotnetAsync(request, settings.OutputFile, settings.Overwrite, cancellationToken));
    }
}
