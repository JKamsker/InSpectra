using System.ComponentModel;
using InSpectra.Gen.Runtime;
using InSpectra.Gen.Services;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Render;

public sealed class ExecHtmlCommand(HtmlRenderService renderService) : AsyncCommand<ExecHtmlSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, ExecHtmlSettings settings, CancellationToken cancellationToken)
    {
        var options = RenderRequestFactory.CreateHtmlOptions(settings, null, null, settings.OutputDirectory, settings.TimeoutSeconds, hasTimeoutSupport: true);
        var features = RenderRequestFactory.CreateHtmlFeatureFlags(settings);
        var workingDirectory = RenderRequestFactory.ResolveWorkingDirectory(settings.WorkingDirectory);
        var request = new ExecRenderRequest(
            settings.Source,
            settings.SourceArguments,
            settings.OpenCliArguments.Length > 0 ? settings.OpenCliArguments : ["cli", "opencli"],
            settings.IncludeXmlDoc || settings.XmlDocArguments.Length > 0,
            settings.XmlDocArguments.Length > 0 ? settings.XmlDocArguments : ["cli", "xmldoc"],
            workingDirectory,
            RenderRequestFactory.ResolveTimeoutSeconds(settings.TimeoutSeconds),
            options);

        return CommandOutputHandler.ExecuteAsync(
            options.OutputMode,
            options.Verbose,
            () => renderService.RenderFromExecAsync(request, features, cancellationToken, settings.Label));
    }
}

/// <summary>
/// Settings for rendering an HTML app bundle by executing a live CLI.
/// </summary>
public sealed class ExecHtmlSettings : HtmlCommandSettingsBase
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
