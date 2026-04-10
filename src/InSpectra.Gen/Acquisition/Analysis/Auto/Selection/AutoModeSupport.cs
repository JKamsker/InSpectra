namespace InSpectra.Gen.Acquisition.Analysis.Auto.Selection;

using InSpectra.Gen.Acquisition.Frameworks;

using InSpectra.Gen.Acquisition.Analysis.Tools;

using InSpectra.Discovery.Tool.Analysis;

internal static class AutoModeSupport
{
    public static IReadOnlyList<AutoAnalysisAttempt> BuildAttemptPlan(ToolDescriptor descriptor)
    {
        if (string.Equals(descriptor.PreferredAnalysisMode, AnalysisMode.Help, StringComparison.OrdinalIgnoreCase))
        {
            return [new AutoAnalysisAttempt(AnalysisMode.Help, null)];
        }

        var attempts = new List<AutoAnalysisAttempt>();
        var hookFrameworks = ResolveHookFrameworks(descriptor);
        foreach (var provider in CliFrameworkProviderRegistry.ResolveAnalysisProviders(descriptor.CliFramework))
        {
            if (provider.SupportsCliFxAnalysis)
            {
                attempts.Add(new AutoAnalysisAttempt(AnalysisMode.CliFx, provider.Name));
            }

            if (provider.SupportsHookAnalysis && hookFrameworks.Contains(provider.Name))
            {
                attempts.Add(new AutoAnalysisAttempt(AnalysisMode.Hook, provider.Name));
            }

            if (provider.StaticAnalysisAdapter is not null)
            {
                attempts.Add(new AutoAnalysisAttempt(AnalysisMode.Static, provider.Name));
            }
        }

        if (attempts.Count == 0)
        {
            return [new AutoAnalysisAttempt(AnalysisMode.Help, null)];
        }

        attempts.Add(new AutoAnalysisAttempt(AnalysisMode.Help, null));
        return attempts;
    }

    public static string ResolveFallbackMode(ToolDescriptor descriptor)
    {
        var attempts = BuildAttemptPlan(descriptor);
        return attempts.Count == 0 ? AnalysisMode.Help : attempts[0].Mode;
    }

    private static HashSet<string> ResolveHookFrameworks(ToolDescriptor descriptor)
    {
        var hookCliFramework = !string.IsNullOrWhiteSpace(descriptor.HookCliFramework)
            ? descriptor.HookCliFramework
            : string.Equals(descriptor.SelectionReason, "candidate-static-analysis-framework", StringComparison.OrdinalIgnoreCase)
                ? null
                : descriptor.CliFramework;
        return CliFrameworkProviderRegistry.ResolveFrameworkNames(hookCliFramework)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
