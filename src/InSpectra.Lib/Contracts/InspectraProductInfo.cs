namespace InSpectra.Lib.Contracts;

internal static class InspectraProductInfo
{
    public const string CliCommandName = "inspectra";
    public const string GeneratorName = "InSpectra.Gen";
    public const string RepositoryRootEnvironmentVariableName = "INSPECTRA_REPO_ROOT";
    public const string LegacyRepositoryRootEnvironmentVariableName = "INSPECTRA_DISCOVERY_REPO_ROOT";

    public static readonly string[] RepositorySolutionFileNames =
    [
        "InSpectra.Gen.sln",
        "InSpectra.Discovery.sln",
    ];
}
