using System.ComponentModel;
using InSpectra.Gen.Runtime;
using InSpectra.Gen.Services;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Render;

public sealed class ExecMarkdownCommand(MarkdownRenderService renderService) : AsyncCommand<ExecRenderSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, ExecRenderSettings settings, CancellationToken cancellationToken)
    {
        var options = RenderRequestFactory.CreateMarkdownOptions(settings, settings.Layout, settings.OutputFile, settings.OutputDirectory, settings.TimeoutSeconds, hasTimeoutSupport: true, splitDepth: settings.SplitDepth);
        var markdownOptions = RenderRequestFactory.CreateMarkdownRenderOptions(settings, options.Layout, settings.SplitDepth);
        var workingDirectory = RenderRequestFactory.ResolveWorkingDirectory(settings.WorkingDirectory);
        var request = new ExecRenderRequest(
            settings.Source,
            settings.SourceArguments,
            settings.OpenCliArguments.Length > 0 ? settings.OpenCliArguments : ["cli", "opencli"],
            settings.IncludeXmlDoc || settings.XmlDocArguments.Length > 0,
            settings.XmlDocArguments.Length > 0 ? settings.XmlDocArguments : ["cli", "xmldoc"],
            workingDirectory,
            RenderRequestFactory.ResolveTimeoutSeconds(settings.TimeoutSeconds),
            options,
            markdownOptions);

        return CommandOutputHandler.ExecuteAsync(
            options.OutputMode,
            options.Verbose,
            () => renderService.RenderFromExecAsync(request, cancellationToken));
    }
}

/// <summary>
/// Settings for rendering Markdown by executing a live CLI.
/// </summary>
public sealed class ExecRenderSettings : MarkdownCommandSettingsBase
{
    /// <summary>
    /// CLI executable or script to invoke for <c>cli opencli</c> exports.
    /// </summary>
    [Description("CLI executable or script to invoke for cli opencli exports.")]
    [CommandArgument(0, "<SOURCE>")]
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Additional arguments passed directly to the source executable before the export command.
    /// </summary>
    [Description("Additional arguments passed directly to the source executable before the export command.")]
    [CommandOption("--source-arg <ARG>")]
    public string[] SourceArguments { get; init; } = [];

    /// <summary>
    /// Override the arguments used to invoke the source CLI's OpenCLI export command.
    /// </summary>
    [Description("Override the arguments used to invoke the source CLI's OpenCLI export command.")]
    [CommandOption("--opencli-arg <ARG>")]
    public string[] OpenCliArguments { get; init; } = [];

    /// <summary>
    /// Also invoke the source CLI's <c>cli xmldoc</c> command for XML enrichment.
    /// </summary>
    [Description("Also invoke the source CLI's cli xmldoc command for XML enrichment.")]
    [CommandOption("--with-xmldoc")]
    public bool IncludeXmlDoc { get; init; }

    /// <summary>
    /// Override the arguments used to invoke the source CLI's XML documentation export command.
    /// </summary>
    [Description("Override the arguments used to invoke the source CLI's XML documentation export command.")]
    [CommandOption("--xmldoc-arg <ARG>")]
    public string[] XmlDocArguments { get; init; } = [];

    /// <summary>
    /// Working directory to use when invoking the source CLI.
    /// </summary>
    [Description("Working directory to use when invoking the source CLI.")]
    [CommandOption("--cwd <PATH>")]
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Timeout in seconds for each export command executed against the source CLI.
    /// </summary>
    [Description("Timeout in seconds for each export command executed against the source CLI.")]
    [CommandOption("--timeout <SECONDS>")]
    public int? TimeoutSeconds { get; init; }
}
