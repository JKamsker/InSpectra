using InSpectra.Gen.Engine.Contracts.Providers;

namespace InSpectra.Gen.Engine.Targets.Sources;

internal sealed class PackageCliTargetFactory(IPackageCliToolInstaller installer)
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
        var installation = await installer.InstallAsync(
            packageId,
            version,
            commandName,
            cliFramework,
            tempRoot,
            timeoutSeconds,
            cancellationToken);

        return new MaterializedCliTarget(
            DisplayName: $"{installation.PackageId}@{installation.Version}",
            CommandPath: installation.CommandPath,
            CommandName: installation.CommandName,
            WorkingDirectory: tempRoot,
            InstallDirectory: installation.InstallDirectory,
            PreferredEntryPointPath: installation.PreferredEntryPointPath,
            Version: installation.Version,
            Environment: installation.Environment,
            CliFramework: installation.CliFramework,
            HookCliFramework: installation.HookCliFramework,
            PackageTitle: installation.PackageTitle,
            PackageDescription: installation.PackageDescription);
    }
}
