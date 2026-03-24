using Microsoft.Extensions.DependencyInjection;
using OpenCli.Renderer.Commands.Render;
using OpenCli.Renderer.Common;
using OpenCli.Renderer.Services;
using Spectre.Console;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddSingleton(AnsiConsole.Console);
services.AddSingleton<OpenCliSchemaProvider>();
services.AddSingleton<OpenCliDocumentLoader>();
services.AddSingleton<OpenCliXmlEnricher>();
services.AddSingleton<OpenCliNormalizer>();
services.AddSingleton<ExecutableResolver>();
services.AddSingleton<ProcessRunner>();
services.AddSingleton<RenderModelFormatter>();
services.AddSingleton<OverviewFormatter>();
services.AddSingleton<CommandPathResolver>();
services.AddSingleton<MarkdownTableRenderer>();
services.AddSingleton<MarkdownMetadataRenderer>();
services.AddSingleton<MarkdownSectionRenderer>();
services.AddSingleton<HtmlAssetProvider>();
services.AddSingleton<HtmlContentFormatter>();
services.AddSingleton<HtmlBlockRenderer>();
services.AddSingleton<HtmlSectionRenderer>();
services.AddSingleton<HtmlShellRenderer>();
services.AddSingleton<MarkdownRenderer>();
services.AddSingleton<HtmlRenderer>();
services.AddSingleton<DocumentRenderService>();

var app = new CommandApp(new TypeRegistrar(services));

app.Configure(config =>
{
    config.SetApplicationName("opencli-renderer");
    config.SetApplicationVersion("0.1.0");

    config.AddBranch("render", render =>
    {
        render.SetDescription("Render documentation from OpenCLI exports.");

        render.AddBranch("file", file =>
        {
            file.SetDescription("Render docs from saved OpenCLI export files.");
            file.AddCommand<FileMarkdownCommand>("markdown")
                .WithDescription("Render Markdown from an OpenCLI JSON file and optional XML enrichment file.");
            file.AddCommand<FileHtmlCommand>("html")
                .WithDescription("Render HTML from an OpenCLI JSON file and optional XML enrichment file.");
        });

        render.AddBranch("exec", exec =>
        {
            exec.SetDescription("Render docs by executing a CLI that exposes `cli opencli`.");
            exec.AddCommand<ExecMarkdownCommand>("markdown")
                .WithDescription("Render Markdown from a live CLI process and optional `cli xmldoc` enrichment.");
            exec.AddCommand<ExecHtmlCommand>("html")
                .WithDescription("Render HTML from a live CLI process and optional `cli xmldoc` enrichment.");
        });
    });
});

return await app.RunAsync(args);
