namespace InSpectra.Gen.Acquisition.Contracts.Providers;

/// <summary>
/// Contracts-level detection result for a locally installed CLI tool. Carries just
/// the framework names the app shell needs to drive acquisition.
/// </summary>
public sealed record LocalCliFrameworkDetection(
    string? CliFramework,
    string? HookCliFramework,
    bool HasManagedAssemblies);

/// <summary>
/// Public composition seam that scans a local install directory and returns which
/// CLI framework(s) the tool ships with.
/// </summary>
public interface ILocalCliFrameworkDetector
{
    /// <summary>
    /// Detects the CLI framework for the managed assemblies under
    /// <paramref name="installDirectory"/>. Returns an empty detection when the
    /// directory does not exist or contains no probable tool assemblies.
    /// </summary>
    LocalCliFrameworkDetection Detect(string installDirectory);
}
