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

    [Fact]
    public void Engine_public_surface_is_limited_to_contracts_use_cases_and_root_composition()
    {
        var violations = typeof(EngineServiceCollectionExtensions).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace is null || !AllowedNamespaces.Contains(type.Namespace))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Unexpected public engine types:\n" + string.Join(Environment.NewLine, violations));
    }
}
