using OpenCli.Renderer.Runtime;
using OpenCli.Renderer.Services;
using Spectre.Console.Cli;

namespace OpenCli.Renderer.Commands.Render;

public sealed class FileMarkdownCommand(MarkdownRenderService renderService) : AsyncCommand<FileMarkdownSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, FileMarkdownSettings settings, CancellationToken cancellationToken)
    {
        var options = RenderRequestFactory.CreateOptions(settings, settings.Layout, settings.OutputFile, settings.OutputDirectory, timeoutSeconds: null, hasTimeoutSupport: false);
        var request = new FileMarkdownRenderRequest(
            settings.OpenCliJsonPath,
            settings.XmlDocPath,
            options);

        return CommandOutputHandler.ExecuteAsync(
            options.OutputMode,
            options.Verbose,
            () => renderService.RenderFromFileAsync(request, cancellationToken));
    }
}

public sealed class FileMarkdownSettings : MarkdownCommandSettingsBase
{
    [CommandArgument(0, "<OPENCLI_JSON>")]
    public string OpenCliJsonPath { get; init; } = string.Empty;

    [CommandOption("--xmldoc <PATH>")]
    public string? XmlDocPath { get; init; }
}
