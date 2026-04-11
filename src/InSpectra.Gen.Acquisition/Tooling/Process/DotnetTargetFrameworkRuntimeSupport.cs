namespace InSpectra.Gen.Acquisition.Tooling.Process;

internal static class DotnetTargetFrameworkRuntimeSupport
{
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

        if (suffix.Length > 0 && !suffix.Contains("windows", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var frameworkName = suffix.Contains("windows", StringComparison.OrdinalIgnoreCase)
            ? "Microsoft.WindowsDesktop.App"
            : "Microsoft.NETCore.App";
        return new DotnetRuntimeRequirement(frameworkName, netChannel + ".0");
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
