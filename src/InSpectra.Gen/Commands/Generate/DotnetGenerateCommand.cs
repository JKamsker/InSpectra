using InSpectra.Gen.Commands.Render;
using InSpectra.Gen.UseCases.Generate.Requests;
using InSpectra.Gen.Output;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Generate;

public sealed class DotnetGenerateCommand(IOpenCliGenerationService generationService) : AsyncCommand<DotnetGenerateSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, DotnetGenerateSettings settings, CancellationToken cancellationToken)
    {
        var outputMode = RenderRequestFactory.ResolveOutputMode(settings);
        var workingDirectory = RenderRequestFactory.ResolveWorkingDirectory(settings.WorkingDirectory);
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
                RenderRequestFactory.ResolveOpenCliMode(settings.OpenCliMode, OpenCliMode.Auto),
                settings.CommandName,
                settings.CliFramework,
                settings.OpenCliArguments.Length > 0 ? settings.OpenCliArguments : ["cli", "opencli"],
                settings.WithXmlDoc,
                settings.XmlDocArguments.Length > 0 ? settings.XmlDocArguments : ["cli", "xmldoc"],
                RenderRequestFactory.ResolveTimeoutSeconds(settings.TimeoutSeconds, defaultSeconds: 120),
                new OpenCliArtifactOptions(null, settings.CrawlOutputPath, settings.Overwrite)));

        return GenerateOutputHandler.ExecuteAsync(
            outputMode,
            settings.Verbose,
            () => generationService.GenerateFromDotnetAsync(request, settings.OutputFile, settings.Overwrite, cancellationToken));
    }
}
