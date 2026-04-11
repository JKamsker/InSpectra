using InSpectra.Gen.Acquisition.Tooling.Tools;
using InSpectra.Gen.Acquisition.Tooling.Process;
using InSpectra.Gen.Acquisition.Tooling.Paths;
using InSpectra.Gen.Acquisition.Contracts.Exceptions;

namespace InSpectra.Gen.Targets;

internal sealed class PackageCliTargetFactory(CommandRuntime runtime, IToolDescriptorResolver toolDescriptorResolver)
{
    internal async Task<MaterializedCliTarget> CreateAsync(
        string packageId,
        string version,
        string? commandName,
        string? cliFramework,
        string tempRoot,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var resolution = await toolDescriptorResolver.ResolveAsync(packageId, version, commandName, cancellationToken);
        var descriptor = resolution.Descriptor;
        var resolvedCommandName = descriptor.CommandName
            ?? throw new CliSourceExecutionException(
                $"Resolved package `{packageId}` {version}, but no tool command name was available.");

        var sandbox = runtime.CreateSandboxEnvironment(tempRoot);
        EnsureDirectories(sandbox.Directories);

        var installDirectory = Path.Combine(tempRoot, "tool");
        var installResult = await runtime.InvokeProcessCaptureAsync(
            "dotnet",
            ["tool", "install", packageId, "--version", version, "--tool-path", installDirectory],
            tempRoot,
            sandbox.Values,
            timeoutSeconds,
            tempRoot,
            cancellationToken);

        if (installResult.TimedOut || installResult.ExitCode != 0)
        {
            var details = new List<string>();
            var stdout = CommandRuntime.NormalizeConsoleText(installResult.Stdout);
            var stderr = CommandRuntime.NormalizeConsoleText(installResult.Stderr);
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                details.Add(stdout);
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                details.Add(stderr);
            }

            throw new CliSourceExecutionException(
                $"Failed to install `{packageId}` {version} as a local .NET tool.",
                details: details);
        }

        var commandPath = runtime.ResolveInstalledCommandPath(installDirectory, resolvedCommandName);
        if (string.IsNullOrWhiteSpace(commandPath))
        {
            throw new CliSourceExecutionException(
                $"Installed package `{packageId}` {version}, but command `{resolvedCommandName}` was not found.",
                "source_not_found");
        }

        var installedCommand = InstalledDotnetToolCommandSupport.TryResolve(installDirectory, resolvedCommandName);
        var effectiveCliFramework = string.IsNullOrWhiteSpace(cliFramework)
            ? descriptor.CliFramework
            : cliFramework.Trim();
        var hookCliFramework = string.IsNullOrWhiteSpace(cliFramework)
            ? descriptor.HookCliFramework
            : cliFramework.Trim();

        return new MaterializedCliTarget(
            DisplayName: $"{packageId}@{version}",
            CommandPath: commandPath,
            CommandName: resolvedCommandName,
            WorkingDirectory: tempRoot,
            InstallDirectory: installDirectory,
            PreferredEntryPointPath: installedCommand?.EntryPointPath,
            Version: version,
            Environment: sandbox.Values,
            CliFramework: effectiveCliFramework,
            HookCliFramework: hookCliFramework,
            PackageTitle: descriptor.PackageTitle,
            PackageDescription: descriptor.PackageDescription);
    }

    private static void EnsureDirectories(IReadOnlyList<string> directories)
    {
        foreach (var directory in directories)
        {
            Directory.CreateDirectory(directory);
        }
    }
}
