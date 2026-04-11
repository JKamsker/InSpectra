using InSpectra.Gen.UseCases.Render;
using InSpectra.Gen.Output;
using InSpectra.Gen.Rendering.Contracts;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Render;

public sealed class FileMarkdownCommand(MarkdownRenderService renderService) : AsyncCommand<FileRenderSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, FileRenderSettings settings, CancellationToken cancellationToken)
    {
        var options = RenderRequestFactory.CreateMarkdownOptions(settings, settings.Layout, settings.OutputFile, settings.OutputDirectory, timeoutSeconds: null, hasTimeoutSupport: false, splitDepth: settings.SplitDepth);
        var markdownOptions = RenderRequestFactory.CreateMarkdownRenderOptions(settings, options.Layout, settings.SplitDepth);
        var request = new FileRenderRequest(
            settings.OpenCliJsonPath,
            settings.XmlDocPath,
            options,
            markdownOptions);

        return CommandOutputHandler.ExecuteAsync(
            options.OutputMode,
            options.Verbose,
            () => renderService.RenderFromFileAsync(request, cancellationToken));
    }
}
