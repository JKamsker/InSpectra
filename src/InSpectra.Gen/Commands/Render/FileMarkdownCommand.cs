using InSpectra.Gen.Commands.Common;
using InSpectra.Gen.Cli.Output;
using InSpectra.Lib.Rendering.Contracts;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Render;

public sealed class FileMarkdownCommand(IMarkdownRenderService renderService) : AsyncCommand<FileRenderSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, FileRenderSettings settings, CancellationToken cancellationToken)
    {
        var outputMode = CommandValueResolver.ResolveOutputMode(settings.Json, settings.Output);
        var quiet = CommandValueResolver.ResolveFlag(settings.Quiet, "INSPECTRA_GEN_QUIET");
        var verbose = CommandValueResolver.ResolveFlag(settings.Verbose, "INSPECTRA_GEN_VERBOSE");
        var options = RenderRequestFactory.CreateMarkdownOptions(settings, outputMode, settings.Layout, settings.OutputFile, settings.OutputDirectory, timeoutSeconds: null, hasTimeoutSupport: false, splitDepth: settings.SplitDepth);
        var markdownOptions = RenderRequestFactory.CreateMarkdownRenderOptions(settings, options.Layout, settings.SplitDepth);
        var request = new FileRenderRequest(
            settings.OpenCliJsonPath,
            settings.XmlDocPath,
            options,
            markdownOptions);

        return RenderOutputHandler.ExecuteAsync(
            outputMode,
            quiet,
            verbose,
            () => renderService.RenderFromFileAsync(request, cancellationToken));
    }
}
