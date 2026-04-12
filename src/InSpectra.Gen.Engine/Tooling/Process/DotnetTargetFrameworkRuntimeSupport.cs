namespace InSpectra.Gen.Engine.Tooling.Process;

using InSpectra.Gen.Engine.Tooling.Json;
using System.Text.Json.Nodes;

internal static class DotnetTargetFrameworkRuntimeSupport
{
    public static bool TryResolveTargetFrameworkMonikerFromSettingsPath(
        string settingsPath,
        out string targetFrameworkMoniker)
    {
        targetFrameworkMoniker = string.Empty;
        if (string.IsNullOrWhiteSpace(settingsPath))
        {
            return false;
        }

        try
        {
            var settingsDirectory = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(settingsPath))!);
            var runtimeIdentifierDirectory = settingsDirectory;
            var frameworkDirectory = runtimeIdentifierDirectory.Parent;
            var toolsDirectory = frameworkDirectory?.Parent;
            if (frameworkDirectory is null
                || toolsDirectory is null
                || !string.Equals(toolsDirectory.Name, "tools", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(frameworkDirectory.Name))
            {
                return false;
            }

            targetFrameworkMoniker = frameworkDirectory.Name;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static DotnetRuntimeRequirement? TryResolveRequirement(string targetFrameworkMoniker)
    {
        if (string.IsNullOrWhiteSpace(targetFrameworkMoniker))
        {
            return null;
        }

        var normalized = targetFrameworkMoniker.Trim();
        var hyphenIndex = normalized.IndexOf('-');
        var suffix = hyphenIndex >= 0 ? normalized[hyphenIndex..] : string.Empty;
        var baseMoniker = hyphenIndex >= 0 ? normalized[..hyphenIndex] : normalized;

        if (baseMoniker.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase)
            && TryParseSupportedChannel(baseMoniker["netcoreapp".Length..], out var netCoreChannel))
        {
            return new DotnetRuntimeRequirement("Microsoft.NETCore.App", netCoreChannel + ".0");
        }

        if (!baseMoniker.StartsWith("net", StringComparison.OrdinalIgnoreCase)
            || !TryParseSupportedChannel(baseMoniker["net".Length..], out var netChannel))
        {
            return null;
        }

        if (suffix.Length > 0)
        {
            return null;
        }

        return new DotnetRuntimeRequirement("Microsoft.NETCore.App", netChannel + ".0");
    }

    public static IReadOnlyList<DotnetRuntimeRequirement> ResolveRequirementsFromRuntimeConfig(string entryPointPath)
    {
        if (string.IsNullOrWhiteSpace(entryPointPath))
        {
            return [];
        }

        var runtimeConfig = JsonNodeFileLoader.TryLoadJsonObject(Path.ChangeExtension(entryPointPath, ".runtimeconfig.json"));
        if (runtimeConfig is null)
        {
            return [];
        }

        var requirements = new List<DotnetRuntimeRequirement>();
        var runtimeOptions = runtimeConfig["runtimeOptions"] as JsonObject;
        AddRequirementIfPresent(requirements, runtimeOptions?["framework"]);

        if (runtimeOptions?["frameworks"] is JsonArray frameworks)
        {
            foreach (var frameworkNode in frameworks)
            {
                AddRequirementIfPresent(requirements, frameworkNode);
            }
        }

        return requirements;
    }

    private static void AddRequirementIfPresent(ICollection<DotnetRuntimeRequirement> requirements, JsonNode? frameworkNode)
    {
        var requirement = TryResolveRequirementFromFrameworkNode(frameworkNode);
        if (requirement is null
            || requirements.Any(existing =>
                string.Equals(existing.Name, requirement.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.Version, requirement.Version, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        requirements.Add(requirement);
    }

    private static DotnetRuntimeRequirement? TryResolveRequirementFromFrameworkNode(JsonNode? frameworkNode)
    {
        var framework = frameworkNode as JsonObject;
        var frameworkName = framework?["name"]?.GetValue<string>();
        var frameworkVersion = framework?["version"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(frameworkName)
            || string.IsNullOrWhiteSpace(frameworkVersion))
        {
            return null;
        }

        return new DotnetRuntimeRequirement(frameworkName, frameworkVersion);
    }

    private static bool TryParseSupportedChannel(string value, out string channel)
    {
        channel = string.Empty;
        var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var major)
            || !int.TryParse(parts[1], out var minor))
        {
            return false;
        }

        if (major < 2)
        {
            return false;
        }

        var normalized = $"{major}.{minor}";
        if (major < 5
            && !string.Equals(normalized, "2.1", StringComparison.Ordinal)
            && !string.Equals(normalized, "2.2", StringComparison.Ordinal)
            && !string.Equals(normalized, "3.0", StringComparison.Ordinal)
            && !string.Equals(normalized, "3.1", StringComparison.Ordinal))
        {
            return false;
        }

        channel = normalized;
        return true;
    }
}
