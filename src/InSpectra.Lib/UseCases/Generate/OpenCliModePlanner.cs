using InSpectra.Lib.Contracts;
using InSpectra.Lib.Contracts.Providers;
using InSpectra.Lib.UseCases.Generate.Requests;

namespace InSpectra.Lib.UseCases.Generate;

internal static class OpenCliModePlanner
{
    public static IReadOnlyList<OpenCliAcquisitionAttempt> BuildAutoPlan(
        ICliFrameworkCatalog catalog,
        string? cliFramework,
        string? hookCliFramework)
    {
        var attempts = new List<OpenCliAcquisitionAttempt>();
        var hookFrameworks = catalog.ResolveFrameworkNames(hookCliFramework)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in catalog.ResolveAnalysisProviders(cliFramework))
        {
            if (provider.SupportsCliFxAnalysis)
            {
                attempts.Add(new OpenCliAcquisitionAttempt(AnalysisMode.CliFx, provider.Name, AnalysisDisposition.Planned));
            }

            if (provider.SupportsHookAnalysis && hookFrameworks.Contains(provider.Name))
            {
                attempts.Add(new OpenCliAcquisitionAttempt(AnalysisMode.Hook, provider.Name, AnalysisDisposition.Planned));
            }

            if (provider.SupportsStaticAnalysis)
            {
                attempts.Add(new OpenCliAcquisitionAttempt(AnalysisMode.Static, provider.Name, AnalysisDisposition.Planned));
            }
        }

        if (attempts.Count == 0)
        {
            attempts.Add(new OpenCliAcquisitionAttempt(AnalysisMode.Help, null, AnalysisDisposition.Planned));
            return attempts;
        }

        attempts.Add(new OpenCliAcquisitionAttempt(AnalysisMode.Help, null, AnalysisDisposition.Planned));
        return attempts;
    }

    public static string ToModeValue(OpenCliMode mode)
        => mode switch
        {
            OpenCliMode.Native => AnalysisMode.Native,
            OpenCliMode.Auto => AnalysisMode.Auto,
            OpenCliMode.Help => AnalysisMode.Help,
            OpenCliMode.CliFx => AnalysisMode.CliFx,
            OpenCliMode.Static => AnalysisMode.Static,
            OpenCliMode.Hook => AnalysisMode.Hook,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };
}
