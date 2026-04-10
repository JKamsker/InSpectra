using InSpectra.Gen.Runtime;
using InSpectra.Gen.Runtime.Acquisition;
using InSpectra.Gen.Runtime.Output;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Generate;

public sealed class DotnetGenerateCommand(IOpenCliGenerationService generationService) : AsyncCommand<DotnetGenerateSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, DotnetGenerateSettings settings, CancellationToken cancellationToken)
    {
        var outputMode = RenderRequestFactory.ResolveOutputMode(settings);
        var workingDirectory = RenderRequestFactory.ResolveWorkingDirectory(settings.WorkingDirectory);
        var request = new DotnetAcquisitionRequest(
            DotnetProjectResolver.Resolve(settings.Project, workingDirectory),
            settings.Configuration,
            settings.Framework,
            settings.LaunchProfile,
            settings.NoBuild,
            settings.NoRestore,
            RenderRequestFactory.ResolveOpenCliMode(settings.OpenCliMode, OpenCliMode.Auto),
            settings.CommandName,
            settings.CliFramework,
            settings.OpenCliArguments.Length > 0 ? settings.OpenCliArguments : ["cli", "opencli"],
            settings.WithXmlDoc,
            settings.XmlDocArguments.Length > 0 ? settings.XmlDocArguments : ["cli", "xmldoc"],
            workingDirectory,
            RenderRequestFactory.ResolveTimeoutSeconds(settings.TimeoutSeconds, defaultSeconds: 120),
            new OpenCliArtifactOptions(null, settings.CrawlOutputPath, settings.Overwrite));

        return GenerateOutputHandler.ExecuteAsync(
            outputMode,
            settings.Verbose,
            () => generationService.GenerateFromDotnetAsync(request, settings.OutputFile, settings.Overwrite, cancellationToken));
    }
}
