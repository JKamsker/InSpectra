using System.ComponentModel;
using InSpectra.Gen.Runtime;
using InSpectra.Gen.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Render;

public sealed class SelfDocCommand(
    IAnsiConsole console,
    MarkdownRenderService markdownService,
    HtmlRenderService htmlService,
    ProcessRunner processRunner) : AsyncCommand<SelfDocSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SelfDocSettings settings, CancellationToken cancellationToken)
    {
        var outputDirectory = Path.GetFullPath(settings.OutputDirectory
            ?? throw new CliUsageException("`--out-dir` is required for `render self`."));

        OutputPathHelper.PrepareDirectory(outputDirectory, settings.Overwrite);

        var executablePath = Environment.ProcessPath
            ?? throw new CliUsageException("Could not determine the current process path.");

        console.MarkupLine($"[grey]Exporting OpenCLI spec from self...[/]");
        var openCliResult = await processRunner.RunAsync(
            executablePath,
            Directory.GetCurrentDirectory(),
            ["cli", "opencli"],
            timeoutSeconds: 30,
            cancellationToken);

        var openCliJson = OpenCliJsonSanitizer.Sanitize(openCliResult.StandardOutput);

        console.MarkupLine($"[grey]Exporting XML doc from self...[/]");
        var xmlDocResult = await processRunner.RunAsync(
            executablePath,
            Directory.GetCurrentDirectory(),
            ["cli", "xmldoc"],
            timeoutSeconds: 30,
            cancellationToken);

        var xmlDocXml = xmlDocResult.StandardOutput;

        // Write raw exports
        var openCliPath = Path.Combine(outputDirectory, "opencli.json");
        var xmlDocPath = Path.Combine(outputDirectory, "xmldoc.xml");
        await File.WriteAllTextAsync(openCliPath, openCliJson, cancellationToken);
        await File.WriteAllTextAsync(xmlDocPath, xmlDocXml, cancellationToken);
        console.MarkupLine($"[green]\u2713[/] Wrote [bold]opencli.json[/] and [bold]xmldoc.xml[/]");

        var fileRequest = new FileRenderRequest(openCliPath, xmlDocPath,
            new RenderExecutionOptions(
                RenderLayout.App,
                ResolvedOutputMode.Human,
                DryRun: false,
                Quiet: true,
                Verbose: false,
                NoColor: false,
                IncludeHidden: settings.IncludeHidden,
                IncludeMetadata: settings.IncludeMetadata,
                Overwrite: true,
                SingleFile: false,
                CompressLevel: Math.Clamp(settings.CompressionLevel ?? 2, 0, 2),
                OutputFile: null,
                OutputDirectory: null));

        // Render Markdown (tree layout)
        if (!settings.SkipMarkdown)
        {
            var treeDir = Path.Combine(outputDirectory, "tree");
            var treeRequest = new FileRenderRequest(openCliPath, xmlDocPath,
                fileRequest.Options with
                {
                    Layout = RenderLayout.Tree,
                    OutputDirectory = treeDir,
                });

            var treeResult = await markdownService.RenderFromFileAsync(treeRequest, cancellationToken);
            console.MarkupLine($"[green]\u2713[/] Wrote [bold]{treeResult.Files.Count}[/] Markdown files to [bold]tree/[/]");
        }

        // Render HTML
        if (!settings.SkipHtml)
        {
            var htmlDir = Path.Combine(outputDirectory, "html");
            var htmlRequest = new FileRenderRequest(openCliPath, xmlDocPath,
                fileRequest.Options with { OutputDirectory = htmlDir });
            var features = RenderRequestFactory.CreateHtmlFeatureFlags(settings);
            var themeOptions = RenderRequestFactory.CreateHtmlThemeOptions(settings);
            var htmlResult = await htmlService.RenderFromFileAsync(htmlRequest, features, cancellationToken, settings.Label, settings.Title, settings.CommandPrefix, themeOptions);
            console.MarkupLine($"[green]\u2713[/] Wrote [bold]{htmlResult.Files.Count}[/] HTML files to [bold]html/[/]");
        }

        console.MarkupLine($"Self-documentation written to [bold]{outputDirectory}[/].");
        return 0;
    }

}

/// <summary>
/// Settings for rendering InSpectra's own documentation set.
/// </summary>
public sealed class SelfDocSettings : SelfDocHtmlCommandSettingsBase
{
    /// <summary>
    /// Skip generating the Markdown tree output under <c>tree/</c>.
    /// </summary>
    [Description("Skip generating the Markdown tree output under tree/.")]
    [CommandOption("--skip-markdown")]
    public bool SkipMarkdown { get; init; }

    /// <summary>
    /// Skip generating the HTML app bundle under <c>html/</c>.
    /// </summary>
    [Description("Skip generating the HTML app bundle under html/.")]
    [CommandOption("--skip-html")]
    public bool SkipHtml { get; init; }
}
