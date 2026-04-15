using InSpectra.Gen.Commands.Common;
using InSpectra.Gen.Cli.Output;
using InSpectra.Lib.Rendering.Contracts;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Render;

public sealed class FileHtmlCommand(IHtmlRenderService renderService) : AsyncCommand<FileHtmlSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, FileHtmlSettings settings, CancellationToken cancellationToken)
    {
        var outputMode = CommandValueResolver.ResolveOutputMode(settings.Json, settings.Output);
        var quiet = CommandValueResolver.ResolveFlag(settings.Quiet, "INSPECTRA_GEN_QUIET");
        var verbose = CommandValueResolver.ResolveFlag(settings.Verbose, "INSPECTRA_GEN_VERBOSE");
        var options = RenderRequestFactory.CreateHtmlOptions(settings, null, null, settings.OutputDirectory, timeoutSeconds: null, hasTimeoutSupport: false);
        var features = RenderRequestFactory.CreateHtmlFeatureFlags(settings);
        var themeOptions = RenderRequestFactory.CreateHtmlThemeOptions(settings);
        var request = new FileRenderRequest(
            settings.OpenCliJsonPath,
            settings.XmlDocPath,
            options);

        return RenderOutputHandler.ExecuteAsync(
            outputMode,
            quiet,
            verbose,
            () => renderService.RenderFromFileAsync(request, features, cancellationToken, settings.Label, settings.Title, settings.CommandPrefix, themeOptions));
    }
}
