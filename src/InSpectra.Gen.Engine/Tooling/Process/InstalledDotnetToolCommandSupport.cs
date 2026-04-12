namespace InSpectra.Gen.Engine.Tooling.Process;

using System.Xml.Linq;

internal static class InstalledDotnetToolCommandSupport
{
    private static readonly StringComparer PathTieBreakerComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public static InstalledDotnetToolCommand? TryResolve(string installDirectory, string commandName)
    {
        if (string.IsNullOrWhiteSpace(installDirectory)
            || string.IsNullOrWhiteSpace(commandName)
            || !Directory.Exists(installDirectory))
        {
            return null;
        }

        var hostRuntimes = HostRuntimeCatalog.Discover();
        return Directory
            .EnumerateFiles(installDirectory, "DotnetToolSettings.xml", SearchOption.AllDirectories)
            .Select(settingsPath => TryCreateCandidate(settingsPath, commandName, hostRuntimes))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .OrderByDescending(candidate => candidate.Compatibility)
            .ThenBy(candidate => candidate.CompatibilityPreference)
            .ThenBy(candidate => candidate.Command.SettingsPath, PathTieBreakerComparer)
            .ThenBy(candidate => candidate.Command.EntryPointPath, PathTieBreakerComparer)
            .Select(candidate => candidate.Command)
            .FirstOrDefault();
    }

    private static InstalledDotnetToolCommand? TryResolveFromSettings(string settingsPath, string commandName)
    {
        try
        {
            var document = XDocument.Load(settingsPath);
            var commandElement = document
                .Descendants()
                .FirstOrDefault(element =>
                    string.Equals(element.Name.LocalName, "Command", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        element.Attribute("Name")?.Value,
                        commandName,
                        StringComparison.OrdinalIgnoreCase));
            if (commandElement is null)
            {
                return null;
            }

            var runner = commandElement.Attribute("Runner")?.Value?.Trim();
            var entryPoint = commandElement.Attribute("EntryPoint")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(entryPoint))
            {
                return null;
            }

            var settingsDirectory = Path.GetDirectoryName(settingsPath);
            if (string.IsNullOrWhiteSpace(settingsDirectory))
            {
                return null;
            }

            var entryPointPath = Path.GetFullPath(Path.Combine(
                settingsDirectory,
                entryPoint.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));
            return new InstalledDotnetToolCommand(
                commandName,
                runner,
                entryPointPath,
                settingsPath);
        }
        catch
        {
            return null;
        }
    }

    private static InstalledDotnetToolCommandCandidate? TryCreateCandidate(
        string settingsPath,
        string commandName,
        HostRuntimeCatalog hostRuntimes)
    {
        var command = TryResolveFromSettings(settingsPath, commandName);
        if (command is null)
        {
            return null;
        }

        var targetFrameworkMoniker = DotnetTargetFrameworkRuntimeSupport
            .TryResolveTargetFrameworkMonikerFromSettingsPath(settingsPath, out var resolvedTargetFrameworkMoniker)
            ? resolvedTargetFrameworkMoniker
            : null;
        var requirements = DotnetTargetFrameworkRuntimeSupport.ResolveRequirementsFromRuntimeConfig(command.EntryPointPath);
        if (requirements.Count == 0
            && !string.IsNullOrWhiteSpace(targetFrameworkMoniker)
            && DotnetTargetFrameworkRuntimeSupport.TryResolveRequirement(targetFrameworkMoniker) is { } requirement)
        {
            requirements = [requirement];
        }

        var compatibility = hostRuntimes.EvaluateCompatibility(requirements, out var compatibilityPreference);

        return new InstalledDotnetToolCommandCandidate(
            command,
            compatibility,
            compatibilityPreference);
    }

    private static Version? TryParseVersion(string? value)
        => Version.TryParse(value, out var version) ? version : null;

    private enum CandidateCompatibility
    {
        Incompatible = 0,
        Unknown = 1,
        Compatible = 2,
    }

    private sealed record InstalledDotnetToolCommandCandidate(
        InstalledDotnetToolCommand Command,
        CandidateCompatibility Compatibility,
        int CompatibilityPreference);

    private sealed class HostRuntimeCatalog
    {
        private readonly bool _hasSharedFrameworkInventory;
        private readonly Dictionary<string, Dictionary<int, Version>> _versionsByFrameworkName;

        private HostRuntimeCatalog(
            bool hasSharedFrameworkInventory,
            Dictionary<string, Dictionary<int, Version>> versionsByFrameworkName)
        {
            _hasSharedFrameworkInventory = hasSharedFrameworkInventory;
            _versionsByFrameworkName = versionsByFrameworkName;
        }

        public static HostRuntimeCatalog Discover()
        {
            var versionsByFrameworkName = new Dictionary<string, Dictionary<int, Version>>(StringComparer.OrdinalIgnoreCase);
            var dotnetRoot = DotnetHostPathResolutionSupport.TryResolveDotnetRootDirectory(
                DotnetHostPathResolutionSupport.TryResolveDotnetHostPath());
            if (!string.IsNullOrWhiteSpace(dotnetRoot))
            {
                AddSharedFrameworkVersions(versionsByFrameworkName, dotnetRoot);
            }

            return new HostRuntimeCatalog(
                hasSharedFrameworkInventory: versionsByFrameworkName.Count > 0,
                versionsByFrameworkName);
        }

        public CandidateCompatibility EvaluateCompatibility(
            IReadOnlyList<DotnetRuntimeRequirement> requirements,
            out int compatibilityPreference)
        {
            if (requirements.Count == 0)
            {
                compatibilityPreference = int.MaxValue;
                return CandidateCompatibility.Unknown;
            }

            var versionPreferences = requirements
                .Select(requirement => TryParseVersion(requirement.Version))
                .Where(version => version is not null)
                .Select(version => ToFrameworkPreference(version!))
                .ToArray();
            var conservativePreference = versionPreferences.Length > 0
                ? versionPreferences.Min()
                : int.MaxValue;
            var compatiblePreference = versionPreferences.Length > 0
                ? -versionPreferences.Max()
                : int.MaxValue;
            var requirementCompatibilities = requirements
                .Select(EvaluateRequirementCompatibility)
                .ToArray();

            if (requirementCompatibilities.All(compatibility => compatibility == CandidateCompatibility.Compatible))
            {
                compatibilityPreference = compatiblePreference;
                return CandidateCompatibility.Compatible;
            }

            if (requirementCompatibilities.Any(compatibility => compatibility == CandidateCompatibility.Incompatible))
            {
                compatibilityPreference = conservativePreference;
                return CandidateCompatibility.Incompatible;
            }

            compatibilityPreference = conservativePreference;
            return CandidateCompatibility.Unknown;
        }

        private static int ToFrameworkPreference(Version version)
            => (version.Major * 1000) + Math.Max(version.Minor, 0);

        private CandidateCompatibility EvaluateRequirementCompatibility(DotnetRuntimeRequirement requirement)
        {
            var requiredVersion = TryParseVersion(requirement.Version);
            if (requiredVersion is null)
            {
                return CandidateCompatibility.Unknown;
            }

            if (_versionsByFrameworkName.TryGetValue(requirement.Name, out var versionsByMajor)
                && versionsByMajor.TryGetValue(requiredVersion.Major, out var hostVersion))
            {
                return hostVersion >= requiredVersion
                    ? CandidateCompatibility.Compatible
                    : CandidateCompatibility.Incompatible;
            }

            return _hasSharedFrameworkInventory
                ? CandidateCompatibility.Incompatible
                : CandidateCompatibility.Unknown;
        }

        private static void AddSharedFrameworkVersions(
            IDictionary<string, Dictionary<int, Version>> versionsByFrameworkName,
            string dotnetRoot)
        {
            var sharedPath = Path.Combine(dotnetRoot, "shared");
            if (!Directory.Exists(sharedPath))
            {
                return;
            }

            foreach (var frameworkDirectory in EnumerateDirectoriesSafely(sharedPath))
            {
                var frameworkName = Path.GetFileName(frameworkDirectory);
                if (string.IsNullOrWhiteSpace(frameworkName))
                {
                    continue;
                }

                foreach (var versionDirectory in EnumerateDirectoriesSafely(frameworkDirectory))
                {
                    var versionName = Path.GetFileName(versionDirectory);
                    if (TryParseVersion(versionName) is { } version)
                    {
                        AddVersion(versionsByFrameworkName, frameworkName, version);
                    }
                }
            }
        }

        private static void AddVersion(
            IDictionary<string, Dictionary<int, Version>> versionsByFrameworkName,
            string frameworkName,
            Version version)
        {
            if (!versionsByFrameworkName.TryGetValue(frameworkName, out var versionsByMajor))
            {
                versionsByMajor = new Dictionary<int, Version>();
                versionsByFrameworkName[frameworkName] = versionsByMajor;
            }

            if (!versionsByMajor.TryGetValue(version.Major, out var currentVersion)
                || version > currentVersion)
            {
                versionsByMajor[version.Major] = version;
            }
        }

        private static IEnumerable<string> EnumerateDirectoriesSafely(string path)
        {
            try
            {
                return Directory.GetDirectories(path);
            }
            catch (IOException)
            {
                return [];
            }
            catch (UnauthorizedAccessException)
            {
                return [];
            }
        }
    }
}

internal sealed record InstalledDotnetToolCommand(
    string CommandName,
    string? Runner,
    string EntryPointPath,
    string SettingsPath);
