using Microsoft.Extensions.DependencyInjection;
using InSpectra.Gen.Acquisition.Analysis.CliFx.Execution;
using InSpectra.Gen.Acquisition.Analysis.CliFx.Metadata;
using InSpectra.Gen.Acquisition.Analysis.CliFx.OpenCli;
using InSpectra.Gen.Acquisition.Analysis.Hook;
using InSpectra.Gen.Acquisition.Help.Crawling;
using InSpectra.Gen.Acquisition.Help.OpenCli;
using InSpectra.Gen.Acquisition.Infrastructure.Commands;
using InSpectra.Gen.Acquisition.StaticAnalysis.Inspection;
using InSpectra.Gen.Acquisition.StaticAnalysis.OpenCli;
using InSpectra.Gen.Commands.Generate;
using InSpectra.Gen.Commands.Render;
using InSpectra.Gen.Common;
using InSpectra.Gen.Services;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddSingleton<OpenCliSchemaProvider>();
services.AddSingleton<OpenCliDocumentLoader>();
services.AddSingleton<OpenCliDocumentCloner>();
services.AddSingleton<OpenCliDocumentSerializer>();
services.AddSingleton<OpenCliXmlEnricher>();
services.AddSingleton<OpenCliNormalizer>();
services.AddSingleton<ExecutableResolver>();
services.AddSingleton<IProcessRunner, ProcessRunner>();
services.AddSingleton<LocalCliFrameworkDetector>();
services.AddSingleton<LocalCliTargetFactory>();
services.AddSingleton<PackageCliTargetFactory>();
services.AddSingleton<DotnetBuildOutputResolver>();
services.AddSingleton<CommandRuntime>();
services.AddSingleton<OpenCliBuilder>();
services.AddSingleton<CliFxMetadataInspector>();
services.AddSingleton<CliFxOpenCliBuilder>();
services.AddSingleton<CliFxCoverageClassifier>();
services.AddSingleton<StaticAnalysisRuntime>();
services.AddSingleton<DnlibAssemblyScanner>();
services.AddSingleton<StaticAnalysisAssemblyInspectionSupport>();
services.AddSingleton<StaticAnalysisOpenCliBuilder>();
services.AddSingleton<StaticAnalysisCoverageClassifier>();
services.AddSingleton<InstalledToolAnalyzer>();
services.AddSingleton<CliFxInstalledToolAnalysisSupport>();
services.AddSingleton<StaticInstalledToolAnalysisSupport>();
services.AddSingleton<HookInstalledToolAnalysisSupport>();
services.AddSingleton<AcquisitionAnalyzerService>();
services.AddSingleton<IOpenCliAcquisitionService, OpenCliAcquisitionService>();
services.AddSingleton<IOpenCliGenerationService, OpenCliGenerationService>();
services.Configure<ViewerBundleLocatorOptions>(_ => { });
services.AddSingleton<RenderStatsFactory>();
services.AddSingleton<RenderModelFormatter>();
services.AddSingleton<OverviewFormatter>();
services.AddSingleton<CommandPathResolver>();
services.AddSingleton<MarkdownTableRenderer>();
services.AddSingleton<MarkdownMetadataRenderer>();
services.AddSingleton<MarkdownSectionRenderer>();
services.AddSingleton<MarkdownRenderer>();
services.AddSingleton<IDocumentRenderService, DocumentRenderService>();
services.AddSingleton<IViewerBundleLocator, ViewerBundleLocator>();
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
