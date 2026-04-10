using System.ComponentModel;
using InSpectra.Gen.Runtime;
using InSpectra.Gen.Services;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Generate;

public sealed class DotnetGenerateCommand(IOpenCliGenerationService generationService) : AsyncCommand<DotnetGenerateSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, DotnetGenerateSettings settings, CancellationToken cancellationToken)
    {
        var outputMode = RenderRequestFactory.ResolveOutputMode(settings);
        var workingDirectory = RenderRequestFactory.ResolveWorkingDirectory(settings.WorkingDirectory);
        var request = new DotnetAcquisitionRequest(
            DotnetProjectResolver.Resolve(settings.Project, workingDirectory),
            settings.Configuration,
            settings.Framework,
            settings.LaunchProfile,
            settings.NoBuild,
            settings.NoRestore,
            RenderRequestFactory.ResolveOpenCliMode(settings.OpenCliMode, OpenCliMode.Auto),
            settings.CommandName,
            settings.CliFramework,
            settings.OpenCliArguments.Length > 0 ? settings.OpenCliArguments : ["cli", "opencli"],
            settings.WithXmlDoc,
            settings.XmlDocArguments.Length > 0 ? settings.XmlDocArguments : ["cli", "xmldoc"],
            workingDirectory,
            RenderRequestFactory.ResolveTimeoutSeconds(settings.TimeoutSeconds, defaultSeconds: 120),
            new OpenCliArtifactOptions(null, settings.CrawlOutputPath));

        return GenerateOutputHandler.ExecuteAsync(
            outputMode,
            settings.Verbose,
            () => generationService.GenerateFromDotnetAsync(request, settings.OutputFile, cancellationToken));
    }
}

public sealed class DotnetGenerateSettings : GenerateCommandSettingsBase
{
    [Description("Path to a .NET project file (.csproj/.fsproj/.vbproj) or a directory containing one.")]
    [CommandArgument(0, "<PROJECT>")]
    public string Project { get; init; } = string.Empty;

    [Description("Build configuration passed to dotnet run/build (e.g. Release).")]
    [CommandOption("-c|--configuration <CONFIG>")]
    public string? Configuration { get; init; }

    [Description("Target framework moniker passed to dotnet run/build (e.g. net10.0).")]
    [CommandOption("-f|--framework <TFM>")]
    public string? Framework { get; init; }

    [Description("Launch profile to use for dotnet run native mode.")]
    [CommandOption("--launch-profile <NAME>")]
    public string? LaunchProfile { get; init; }

    [Description("Skip the implicit build step for dotnet run/build.")]
    [CommandOption("--no-build")]
    public bool NoBuild { get; init; }

    [Description("Skip the implicit restore step for dotnet run/build.")]
    [CommandOption("--no-restore")]
    public bool NoRestore { get; init; }

    [Description("Override the arguments used to invoke the project's OpenCLI export command.")]
    [CommandOption("--opencli-arg <ARG>")]
    public string[] OpenCliArguments { get; init; } = [];

    [Description("Working directory used when invoking dotnet.")]
    [CommandOption("--cwd <PATH>")]
    public string? WorkingDirectory { get; init; }

    [Description("Timeout in seconds for dotnet execution.")]
    [CommandOption("--timeout <SECONDS>")]
    public int? TimeoutSeconds { get; init; }
}
