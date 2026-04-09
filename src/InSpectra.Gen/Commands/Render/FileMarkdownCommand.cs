using System.ComponentModel;
using InSpectra.Gen.Runtime;
using InSpectra.Gen.Services;
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

/// <summary>
/// Settings for rendering Markdown from saved OpenCLI export files.
/// </summary>
public sealed class FileRenderSettings : MarkdownCommandSettingsBase
{
    /// <summary>
    /// Path to the OpenCLI JSON export file to render.
    /// </summary>
    [Description("Path to the OpenCLI JSON export file to render.")]
    [CommandArgument(0, "<OPENCLI_JSON>")]
    public string OpenCliJsonPath { get; init; } = string.Empty;

    /// <summary>
    /// Optional XML documentation file used to enrich missing descriptions.
    /// </summary>
    [Description("Optional XML documentation file used to enrich missing descriptions.")]
    [CommandOption("--xmldoc <PATH>")]
    public string? XmlDocPath { get; init; }
}
