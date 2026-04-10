using Microsoft.Extensions.DependencyInjection;
using InSpectra.Gen.Commands.Generate;
using InSpectra.Gen.Commands.Render;
using InSpectra.Gen.Common;
using InSpectra.Gen.Services;
using Spectre.Console;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddSingleton(AnsiConsole.Console);
services.AddSingleton<OpenCliSchemaProvider>();
services.AddSingleton<OpenCliDocumentLoader>();
services.AddSingleton<OpenCliDocumentCloner>();
services.AddSingleton<OpenCliXmlEnricher>();
services.AddSingleton<OpenCliNormalizer>();
services.AddSingleton<ExecutableResolver>();
services.AddSingleton<ProcessRunner>();
services.AddSingleton<LocalCliFrameworkDetector>();
services.AddSingleton<LocalCliTargetFactory>();
services.AddSingleton<PackageCliTargetFactory>();
services.AddSingleton<DotnetBuildOutputResolver>();
services.AddSingleton<AcquisitionAnalyzerService>();
services.AddSingleton<OpenCliAcquisitionService>();
services.AddSingleton<OpenCliGenerationService>();
services.AddSingleton(new ViewerBundleLocatorOptions());
services.AddSingleton<RenderStatsFactory>();
services.AddSingleton<RenderModelFormatter>();
services.AddSingleton<OverviewFormatter>();
services.AddSingleton<CommandPathResolver>();
services.AddSingleton<MarkdownTableRenderer>();
services.AddSingleton<MarkdownMetadataRenderer>();
services.AddSingleton<MarkdownSectionRenderer>();
services.AddSingleton<MarkdownRenderer>();
services.AddSingleton<DocumentRenderService>();
services.AddSingleton<ViewerBundleLocator>();
services.AddSingleton<MarkdownRenderService>();
services.AddSingleton<HtmlRenderService>();

var app = new CommandApp(new TypeRegistrar(services));

app.Configure(config =>
{
    config.SetApplicationName("inspectra");
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
                .WithDescription("Render an HTML app bundle from an OpenCLI JSON file and optional XML enrichment file.");
        });

        render.AddBranch("exec", exec =>
        {
            exec.SetDescription("Render docs by executing a CLI that exposes `cli opencli`.");
            exec.AddCommand<ExecMarkdownCommand>("markdown")
                .WithDescription("Render Markdown from a live CLI process and optional `cli xmldoc` enrichment.");
            exec.AddCommand<ExecHtmlCommand>("html")
                .WithDescription("Render an HTML app bundle from a live CLI process and optional `cli xmldoc` enrichment.");
        });

        render.AddBranch("dotnet", dotnet =>
        {
            dotnet.SetDescription("Render docs by running a .NET project (csproj) that exposes `cli opencli`.");
            dotnet.AddCommand<DotnetMarkdownCommand>("markdown")
                .WithDescription("Render Markdown by invoking `dotnet run --project <csproj> -- cli opencli`.");
            dotnet.AddCommand<DotnetHtmlCommand>("html")
                .WithDescription("Render an HTML app bundle by invoking `dotnet run --project <csproj> -- cli opencli`.");
        });

        render.AddBranch("package", package =>
        {
            package.SetDescription("Render docs by installing and analyzing a .NET tool package from NuGet.");
            package.AddCommand<PackageMarkdownCommand>("markdown")
                .WithDescription("Render Markdown from an analyzed .NET tool package.");
            package.AddCommand<PackageHtmlCommand>("html")
                .WithDescription("Render an HTML app bundle from an analyzed .NET tool package.");
        });

        render.AddCommand<SelfDocCommand>("self")
            .WithDescription("Render documentation for InSpectra itself. Exports opencli.json, xmldoc.xml, Markdown tree, and HTML bundle.");
    });

    config.AddBranch("generate", generate =>
    {
        generate.SetDescription("Generate raw OpenCLI JSON from a package, executable, or .NET project.");
        generate.AddCommand<PackageGenerateCommand>("package")
            .WithDescription("Generate opencli.json by installing and analyzing a .NET tool package.");
        generate.AddCommand<ExecGenerateCommand>("exec")
            .WithDescription("Generate opencli.json from a local executable or script.");
        generate.AddCommand<DotnetGenerateCommand>("dotnet")
            .WithDescription("Generate opencli.json from a .NET project.");
    });
});

return await app.RunAsync(args);
