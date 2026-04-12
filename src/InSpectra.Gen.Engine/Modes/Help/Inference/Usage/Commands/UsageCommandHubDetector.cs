namespace InSpectra.Gen.Engine.Modes.Help.Inference.Usage.Commands;

using System.Runtime.CompilerServices;

using InSpectra.Gen.Engine.Contracts.Providers;

/// <summary>
/// Help-mode implementation of <see cref="IUsageCommandHubDetector"/> backed by the
/// existing <see cref="UsageCommandInferenceSupport"/> heuristic. Registered with
/// <see cref="UsageCommandHubDetectorAccessor"/> at module load so that other modes
/// can reach the Help-mode heuristic without taking a direct cross-mode import.
/// </summary>
internal sealed class UsageCommandHubDetector : IUsageCommandHubDetector
{
    public static UsageCommandHubDetector Instance { get; } = new();

    public bool LooksLikeCommandHub(string rootCommandName, IReadOnlyList<string> usageLines)
        => UsageCommandInferenceSupport.LooksLikeCommandHub(rootCommandName, usageLines);

#pragma warning disable CA2255 // ModuleInitializer keeps the registration cross-mode-safe; see StaticAttributeReaderRegistration.
    [ModuleInitializer]
    internal static void Register()
    {
        UsageCommandHubDetectorAccessor.Set(Instance);
    }
#pragma warning restore CA2255
}
