using InSpectra.Gen.Runtime;
using InSpectra.Gen.Runtime.Output;
using InSpectra.Gen.Runtime.Rendering;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Render;

public sealed class FileHtmlCommand(HtmlRenderService renderService) : AsyncCommand<FileHtmlSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, FileHtmlSettings settings, CancellationToken cancellationToken)
    {
        var options = RenderRequestFactory.CreateHtmlOptions(settings, null, null, settings.OutputDirectory, timeoutSeconds: null, hasTimeoutSupport: false);
        var features = RenderRequestFactory.CreateHtmlFeatureFlags(settings);
        var themeOptions = RenderRequestFactory.CreateHtmlThemeOptions(settings);
        var request = new FileRenderRequest(
            settings.OpenCliJsonPath,
            settings.XmlDocPath,
            options);

        return CommandOutputHandler.ExecuteAsync(
            options.OutputMode,
            options.Verbose,
            () => renderService.RenderFromFileAsync(request, features, cancellationToken, settings.Label, settings.Title, settings.CommandPrefix, themeOptions));
    }
}
