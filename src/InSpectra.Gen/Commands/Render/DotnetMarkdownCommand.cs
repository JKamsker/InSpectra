using System.ComponentModel;
using InSpectra.Gen.Runtime;
using InSpectra.Gen.Services;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Render;

public sealed class DotnetMarkdownCommand(MarkdownRenderService renderService) : AsyncCommand<DotnetMarkdownSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, DotnetMarkdownSettings settings, CancellationToken cancellationToken)
    {
        var options = RenderRequestFactory.CreateMarkdownOptions(settings, settings.Layout, settings.OutputFile, settings.OutputDirectory, settings.TimeoutSeconds, hasTimeoutSupport: true, splitDepth: settings.SplitDepth);
        var markdownOptions = RenderRequestFactory.CreateMarkdownRenderOptions(settings, options.Layout, settings.SplitDepth);
        var workingDirectory = RenderRequestFactory.ResolveWorkingDirectory(settings.WorkingDirectory);
        var resolvedProject = DotnetProjectResolver.Resolve(settings.Project, workingDirectory);
        var sourceArguments = DotnetProjectArgsBuilder.Build(
            resolvedProject,
            settings.Configuration,
            settings.Framework,
            settings.LaunchProfile,
            settings.NoBuild,
            settings.NoRestore);
        var request = new ExecRenderRequest(
            "dotnet",
            sourceArguments,
            settings.OpenCliArguments.Length > 0 ? settings.OpenCliArguments : ["cli", "opencli"],
            settings.IncludeXmlDoc || settings.XmlDocArguments.Length > 0,
            settings.XmlDocArguments.Length > 0 ? settings.XmlDocArguments : ["cli", "xmldoc"],
            workingDirectory,
            RenderRequestFactory.ResolveTimeoutSeconds(settings.TimeoutSeconds, defaultSeconds: 120),
            options,
            markdownOptions);

        return CommandOutputHandler.ExecuteAsync(
            options.OutputMode,
            options.Verbose,
            () => renderService.RenderFromExecAsync(request, cancellationToken));
    }
}

/// <summary>
/// Settings for rendering Markdown by running a .NET project via <c>dotnet run</c>.
/// </summary>
public sealed class DotnetMarkdownSettings : MarkdownCommandSettingsBase
{
    /// <summary>
    /// Path to a .NET project file (.csproj / .fsproj / .vbproj) or a directory containing one.
    /// </summary>
    [Description("Path to a .NET project file (.csproj/.fsproj/.vbproj) or a directory containing one.")]
    [CommandArgument(0, "<PROJECT>")]
    public string Project { get; init; } = string.Empty;

    /// <summary>
    /// Build configuration passed to <c>dotnet run</c> (e.g. Release).
    /// </summary>
    [Description("Build configuration passed to dotnet run (e.g. Release).")]
    [CommandOption("-c|--configuration <CONFIG>")]
    public string? Configuration { get; init; }

    /// <summary>
    /// Target framework moniker passed to <c>dotnet run</c> (e.g. net10.0).
    /// </summary>
    [Description("Target framework moniker passed to dotnet run (e.g. net10.0).")]
    [CommandOption("-f|--framework <TFM>")]
    public string? Framework { get; init; }

    /// <summary>
    /// Launch profile to use for <c>dotnet run</c>.
    /// </summary>
    [Description("Launch profile to use for dotnet run.")]
    [CommandOption("--launch-profile <NAME>")]
    public string? LaunchProfile { get; init; }

    /// <summary>
    /// Skip the implicit build step (<c>dotnet run --no-build</c>). Use after a separate <c>dotnet build</c>.
    /// </summary>
    [Description("Skip the implicit build step (dotnet run --no-build). Use after a separate dotnet build.")]
    [CommandOption("--no-build")]
    public bool NoBuild { get; init; }

    /// <summary>
    /// Skip the implicit restore step (<c>dotnet run --no-restore</c>).
    /// </summary>
    [Description("Skip the implicit restore step (dotnet run --no-restore).")]
    [CommandOption("--no-restore")]
    public bool NoRestore { get; init; }

    /// <summary>
    /// Also invoke the project's <c>cli xmldoc</c> command for XML enrichment.
    /// </summary>
    [Description("Also invoke the project's cli xmldoc command for XML enrichment.")]
    [CommandOption("--with-xmldoc")]
    public bool IncludeXmlDoc { get; init; }

    /// <summary>
    /// Override the arguments used to invoke the project's OpenCLI export command.
    /// </summary>
    [Description("Override the arguments used to invoke the project's OpenCLI export command.")]
    [CommandOption("--opencli-arg <ARG>")]
    public string[] OpenCliArguments { get; init; } = [];

    /// <summary>
    /// Override the arguments used to invoke the project's XML documentation export command.
    /// </summary>
    [Description("Override the arguments used to invoke the project's XML documentation export command.")]
    [CommandOption("--xmldoc-arg <ARG>")]
    public string[] XmlDocArguments { get; init; } = [];

    /// <summary>
    /// Working directory used when invoking <c>dotnet run</c>.
    /// </summary>
    [Description("Working directory used when invoking dotnet run.")]
    [CommandOption("--cwd <PATH>")]
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Timeout in seconds for each <c>dotnet run</c> invocation (default 120).
    /// </summary>
    [Description("Timeout in seconds for each dotnet run invocation (default 120).")]
    [CommandOption("--timeout <SECONDS>")]
    public int? TimeoutSeconds { get; init; }
}
