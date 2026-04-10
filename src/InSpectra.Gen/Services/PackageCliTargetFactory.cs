using InSpectra.Gen.Acquisition.Analysis.Tools;
using InSpectra.Gen.Acquisition.Infrastructure.Commands;
using InSpectra.Gen.Acquisition.Infrastructure.Host;
using InSpectra.Gen.Acquisition.Infrastructure.Paths;
using InSpectra.Gen.Acquisition.Packages;
using InSpectra.Gen.Runtime;
using DiscoveryRuntime = InSpectra.Gen.Acquisition.Infrastructure.Host.Runtime;

namespace InSpectra.Gen.Services;

public sealed class PackageCliTargetFactory
{
    private readonly CommandRuntime _runtime = new();

    internal async Task<MaterializedCliTarget> CreateAsync(
        string packageId,
        string version,
        string? commandName,
        string? cliFramework,
        string tempRoot,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        using var scope = DiscoveryRuntime.CreateNuGetApiClientScope();
        var (leaf, catalogLeaf) = await PackageVersionResolver.ResolveAsync(scope.Client, packageId, version, cancellationToken);
        var inspection = await new PackageArchiveInspector(scope.Client).InspectAsync(leaf.PackageContent, cancellationToken);
        var resolvedCommandName = ResolveCommandName(packageId, commandName, inspection);
        var descriptor = ToolDescriptorResolver.ResolveFromCatalogLeaf(
            packageId,
            version,
            catalogLeaf,
            packageUrl: $"https://www.nuget.org/packages/{packageId}/{version}",
            packageContentUrl: leaf.PackageContent,
            catalogEntryUrl: leaf.CatalogEntryUrl,
            inspection,
            resolvedCommandName);

        var sandbox = _runtime.CreateSandboxEnvironment(tempRoot);
        EnsureDirectories(sandbox.Directories);

        var installDirectory = Path.Combine(tempRoot, "tool");
        var installResult = await _runtime.InvokeProcessCaptureAsync(
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

        var commandPath = _runtime.ResolveInstalledCommandPath(installDirectory, resolvedCommandName);
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

    private static string ResolveCommandName(
        string packageId,
        string? commandName,
        SpectrePackageInspection inspection)
    {
        var commands = inspection.ToolCommandNames
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (commands.Length == 0)
        {
            throw new CliUsageException(
                $"Package `{packageId}` does not expose a .NET tool command in DotnetToolSettings.xml.");
        }

        if (!string.IsNullOrWhiteSpace(commandName))
        {
            var resolved = commands.FirstOrDefault(candidate =>
                string.Equals(candidate, commandName, StringComparison.OrdinalIgnoreCase));
            if (resolved is null)
            {
                throw new CliUsageException(
                    $"Package `{packageId}` does not expose command `{commandName}`. Available commands: {string.Join(", ", commands)}.");
            }

            return resolved;
        }

        if (commands.Length > 1)
        {
            throw new CliUsageException(
                $"Package `{packageId}` exposes multiple tool commands ({string.Join(", ", commands)}). Use `--command` to select one.");
        }

        return commands[0];
    }

    private static void EnsureDirectories(IReadOnlyList<string> directories)
    {
        foreach (var directory in directories)
        {
            Directory.CreateDirectory(directory);
        }
    }
}
