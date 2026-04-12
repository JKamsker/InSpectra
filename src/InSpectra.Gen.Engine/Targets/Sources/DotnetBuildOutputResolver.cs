using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Execution.Process;
using InSpectra.Gen.Engine.UseCases.Generate.Requests;

namespace InSpectra.Gen.Engine.Targets.Sources;

internal sealed record DotnetBuildOutput(
    string TargetPath,
    IReadOnlyList<string> Warnings);

internal sealed class DotnetBuildOutputResolver(IProcessRunner processRunner)
{
    internal async Task<DotnetBuildOutput> ResolveAsync(
        DotnetBuildSettings settings,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        if (!string.IsNullOrWhiteSpace(settings.LaunchProfile))
        {
            warnings.Add("A launch profile only affects `dotnet run`; non-native analysis uses the built output instead.");
        }

        if (!settings.NoBuild)
        {
            var buildArguments = new List<string>
            {
                "build",
                settings.ProjectPath,
                "-nologo",
            };
            AddCommonArguments(buildArguments, settings.Configuration, settings.Framework, settings.NoRestore);
            await processRunner.RunAsync("dotnet", workingDirectory, buildArguments, timeoutSeconds, cancellationToken);
        }

        var targetPath = await ResolveTargetPathAsync(
            settings.ProjectPath,
            settings.Configuration,
            settings.Framework,
            workingDirectory,
            timeoutSeconds,
            cancellationToken);

        if (!File.Exists(targetPath))
        {
            throw new CliSourceExecutionException(
                $"Resolved project output `{targetPath}` does not exist. Build the project first or allow the acquisition workflow to build it.",
                "source_not_found");
        }

        return new DotnetBuildOutput(targetPath, warnings);
    }

    private async Task<string> ResolveTargetPathAsync(
        string projectPath,
        string? configuration,
        string? framework,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "msbuild",
            projectPath,
            "-nologo",
            "-getProperty:TargetPath",
        };

        AddPropertyArgument(arguments, "Configuration", configuration);
        AddPropertyArgument(arguments, "TargetFramework", framework);

        var result = await processRunner.RunAsync("dotnet", workingDirectory, arguments, timeoutSeconds, cancellationToken);
        var targetPath = result.StandardOutput.Trim();
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new CliSourceExecutionException(
                $"`dotnet msbuild` did not return a target path for `{projectPath}`.");
        }

        return Path.GetFullPath(targetPath, workingDirectory);
    }

    private static void AddCommonArguments(
        ICollection<string> arguments,
        string? configuration,
        string? framework,
        bool noRestore)
    {
        if (!string.IsNullOrWhiteSpace(configuration))
        {
            arguments.Add("-c");
            arguments.Add(configuration);
        }

        if (!string.IsNullOrWhiteSpace(framework))
        {
            arguments.Add("-f");
            arguments.Add(framework);
        }

        if (noRestore)
        {
            arguments.Add("--no-restore");
        }
    }

    private static void AddPropertyArgument(ICollection<string> arguments, string propertyName, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            arguments.Add($"-property:{propertyName}={value}");
        }
    }
}
