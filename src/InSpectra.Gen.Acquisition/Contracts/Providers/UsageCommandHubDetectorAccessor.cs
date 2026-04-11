namespace InSpectra.Gen.Acquisition.Contracts.Providers;

/// <summary>
/// Module-wide accessor that hands out the current <see cref="IUsageCommandHubDetector"/>
/// implementation. Help-mode code registers its detector at startup via
/// <see cref="Set"/>; other modes query the registered instance through
/// <see cref="Current"/> without taking a direct cross-mode import.
/// </summary>
internal static class UsageCommandHubDetectorAccessor
{
    private static IUsageCommandHubDetector _current = NullUsageCommandHubDetector.Instance;

    public static IUsageCommandHubDetector Current => _current;

    public static void Set(IUsageCommandHubDetector detector)
    {
        _current = detector ?? NullUsageCommandHubDetector.Instance;
    }

    private sealed class NullUsageCommandHubDetector : IUsageCommandHubDetector
    {
        public static NullUsageCommandHubDetector Instance { get; } = new();

        public bool LooksLikeCommandHub(string rootCommandName, IReadOnlyList<string> usageLines) => false;
    }
}
