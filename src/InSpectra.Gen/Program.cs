using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using InSpectra.Gen.Commands.Generate;
using InSpectra.Gen.Commands.Render;
using InSpectra.Gen.Hosting;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddInSpectraGen();

var app = new CommandApp(new TypeRegistrar(services));
var version = typeof(TypeRegistrar).Assembly
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
    .InformationalVersion
    .Split('+')[0]
    ?? typeof(TypeRegistrar).Assembly.GetName().Version?.ToString()
    ?? "0.0.0";

app.Configure(config =>
{
    config.SetApplicationName("inspectra");
    config.SetApplicationVersion(version);

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
        generate.SetDescription("Generate validated OpenCLI JSON from a package, executable, or .NET project.");
        generate.AddCommand<PackageGenerateCommand>("package")
            .WithDescription("Generate opencli.json by installing and analyzing a .NET tool package.");
        generate.AddCommand<ExecGenerateCommand>("exec")
            .WithDescription("Generate opencli.json from a local executable or script.");
        generate.AddCommand<DotnetGenerateCommand>("dotnet")
            .WithDescription("Generate opencli.json from a .NET project.");
    });
});

return await app.RunAsync(args);
