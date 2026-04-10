using InSpectra.Gen.Acquisition.Frameworks;
using InSpectra.Gen.Runtime.Acquisition;

using InSpectra.Gen.Acquisition.Analysis;

namespace InSpectra.Gen.OpenCli.Acquisition;

internal static class OpenCliModePlanner
{
    public static IReadOnlyList<OpenCliAcquisitionAttempt> BuildAutoPlan(
        string? cliFramework,
        string? hookCliFramework)
    {
        var attempts = new List<OpenCliAcquisitionAttempt>();
        var hookFrameworks = CliFrameworkProviderRegistry.ResolveFrameworkNames(hookCliFramework)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in CliFrameworkProviderRegistry.ResolveAnalysisProviders(cliFramework))
        {
            if (provider.SupportsCliFxAnalysis)
            {
                attempts.Add(new OpenCliAcquisitionAttempt(AnalysisMode.CliFx, provider.Name, AnalysisDisposition.Planned));
            }

            if (provider.SupportsHookAnalysis && hookFrameworks.Contains(provider.Name))
            {
                attempts.Add(new OpenCliAcquisitionAttempt(AnalysisMode.Hook, provider.Name, AnalysisDisposition.Planned));
            }

            if (provider.StaticAnalysisAdapter is not null)
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
