using OpenCli.Renderer.Runtime;
using OpenCli.Renderer.Services;
using Spectre.Console.Cli;

namespace OpenCli.Renderer.Commands.Render;

public sealed class ExecHtmlCommand(HtmlRenderService renderService) : AsyncCommand<ExecRenderSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, ExecRenderSettings settings, CancellationToken cancellationToken)
    {
        var options = RenderRequestFactory.CreateHtmlOptions(settings, settings.Layout, settings.OutputFile, settings.OutputDirectory, settings.TimeoutSeconds, hasTimeoutSupport: true);
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
            () => renderService.RenderFromExecAsync(request, cancellationToken));
    }
}
