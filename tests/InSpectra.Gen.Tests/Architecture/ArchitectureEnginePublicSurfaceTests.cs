using InSpectra.Lib.Composition;

namespace InSpectra.Gen.Tests.Architecture;

public sealed class ArchitectureEnginePublicSurfaceTests
{
    private static readonly HashSet<string> AllowedNamespaces = new(StringComparer.Ordinal)
    {
        "InSpectra.Lib",
        "InSpectra.Lib.Composition",
        "InSpectra.Lib.Contracts",
        "InSpectra.Lib.Contracts.Providers",
        "InSpectra.Lib.Rendering.Contracts",
        "InSpectra.Lib.UseCases.Generate",
        "InSpectra.Lib.UseCases.Generate.Requests",
    };

    private static readonly string[] AllowedNamespacePrefixes =
    [
        "InSpectra.Lib.Tooling.FrameworkDetection",
        "InSpectra.Lib.Tooling.Json",
        "InSpectra.Lib.Tooling.NuGet",
        "InSpectra.Lib.Tooling.Packages",
        "InSpectra.Lib.Tooling.Paths",
        "InSpectra.Lib.Tooling.Process",
        "InSpectra.Lib.Tooling.Tools",
    ];

    [Fact]
    public void Engine_public_surface_is_limited_to_contracts_use_cases_and_root_composition()
    {
        var violations = typeof(EngineServiceCollectionExtensions).Assembly
            .GetExportedTypes()
            .Where(type => !IsAllowedNamespace(type.Namespace))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Unexpected public engine types:\n" + string.Join(Environment.NewLine, violations));
    }

    private static bool IsAllowedNamespace(string? namespaceName)
    {
        if (namespaceName is null)
        {
            return false;
        }

        return AllowedNamespaces.Contains(namespaceName)
            || AllowedNamespacePrefixes.Any(prefix =>
                namespaceName.Equals(prefix, StringComparison.Ordinal)
                || namespaceName.StartsWith(prefix + ".", StringComparison.Ordinal));
    }
}
