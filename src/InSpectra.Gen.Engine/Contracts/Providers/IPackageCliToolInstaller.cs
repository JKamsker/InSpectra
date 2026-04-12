namespace InSpectra.Gen.Engine.Contracts.Providers;

/// <summary>
/// Contracts-level result of installing a NuGet-distributed CLI tool. Carries just the
/// values the app shell needs to build a materialized CLI target without reaching into
/// deep <c>Tooling.Tools</c> and <c>Tooling.Process</c> namespaces.
/// </summary>
public sealed record PackageCliToolInstallation(
    string PackageId,
    string Version,
    string CommandName,
    string CommandPath,
    string InstallDirectory,
    string? PreferredEntryPointPath,
    IReadOnlyDictionary<string, string> Environment,
    string? CliFramework,
    string? HookCliFramework,
    string? PackageTitle,
    string? PackageDescription);

/// <summary>
/// Public composition seam that installs a NuGet-distributed CLI tool into a sandbox
/// directory and returns the values the app shell needs to drive acquisition.
/// </summary>
public interface IPackageCliToolInstaller
{
    /// <summary>
    /// Resolves, downloads, and installs <paramref name="packageId"/> at
    /// <paramref name="version"/> under <paramref name="tempRoot"/>, then returns a
    /// <see cref="PackageCliToolInstallation"/> describing the installed binary.
    /// </summary>
    /// <param name="packageId">NuGet package identifier.</param>
    /// <param name="version">Exact package version to install.</param>
    /// <param name="commandName">
    /// Optional explicit tool command name. When null, the resolver picks the single
    /// command exposed by the package, or throws if the package exposes multiple.
    /// </param>
    /// <param name="cliFramework">Optional CLI framework override.</param>
    /// <param name="tempRoot">Sandbox root where the tool will be installed.</param>
    /// <param name="timeoutSeconds">Per-process timeout for the install command.</param>
    /// <param name="cancellationToken">Cancellation token forwarded to install.</param>
    Task<PackageCliToolInstallation> InstallAsync(
        string packageId,
        string version,
        string? commandName,
        string? cliFramework,
        string tempRoot,
        int timeoutSeconds,
        CancellationToken cancellationToken);
}
