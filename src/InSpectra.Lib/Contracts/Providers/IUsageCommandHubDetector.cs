namespace InSpectra.Lib.Contracts.Providers;

/// <summary>
/// Narrow abstraction over the help-mode heuristic that decides whether a usage-lines
/// block describes a "command hub" (i.e. the root tool dispatches to named subcommands).
/// Exposed in Contracts so Static-mode projection can consult Help-mode inference
/// without taking a direct cross-mode import.
/// </summary>
internal interface IUsageCommandHubDetector
{
    bool LooksLikeCommandHub(string rootCommandName, IReadOnlyList<string> usageLines);
}
