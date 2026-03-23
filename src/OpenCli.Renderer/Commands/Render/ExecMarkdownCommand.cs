using OpenCli.Renderer.Runtime;
using OpenCli.Renderer.Services;
using Spectre.Console.Cli;

namespace OpenCli.Renderer.Commands.Render;

public sealed class ExecMarkdownCommand(MarkdownRenderService renderService) : AsyncCommand<ExecMarkdownSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, ExecMarkdownSettings settings, CancellationToken cancellationToken)
    {
        var options = RenderRequestFactory.CreateOptions(settings, settings.Layout, settings.OutputFile, settings.OutputDirectory, settings.TimeoutSeconds, hasTimeoutSupport: true);
        var workingDirectory = RenderRequestFactory.ResolveWorkingDirectory(settings.WorkingDirectory);
        var request = new ExecMarkdownRenderRequest(
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
            () => renderService.RenderFromExecAsync(request, cancellationToken));
    }
}

public sealed class ExecMarkdownSettings : MarkdownCommandSettingsBase
{
    [CommandArgument(0, "<SOURCE>")]
    public string Source { get; init; } = string.Empty;

    [CommandOption("--source-arg <ARG>")]
    public string[] SourceArguments { get; init; } = [];

    [CommandOption("--opencli-arg <ARG>")]
    public string[] OpenCliArguments { get; init; } = [];

    [CommandOption("--with-xmldoc")]
    public bool IncludeXmlDoc { get; init; }

    [CommandOption("--xmldoc-arg <ARG>")]
    public string[] XmlDocArguments { get; init; } = [];

    [CommandOption("--cwd <PATH>")]
    public string? WorkingDirectory { get; init; }

    [CommandOption("--timeout <SECONDS>")]
    public int? TimeoutSeconds { get; init; }
}
