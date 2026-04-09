using System.ComponentModel;
using InSpectra.Gen.Runtime;
using InSpectra.Gen.Services;
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

/// <summary>
/// Settings for rendering an HTML app bundle from saved OpenCLI export files.
/// </summary>
public sealed class FileHtmlSettings : HtmlCommandSettingsBase
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
