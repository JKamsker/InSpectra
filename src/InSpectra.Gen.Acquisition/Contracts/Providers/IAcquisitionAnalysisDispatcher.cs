namespace InSpectra.Gen.Acquisition.Contracts.Providers;

/// <summary>
/// Public composition seam that lets the app shell run the correct installed-tool
/// analyzer for a given <see cref="AnalysisMode"/> without reaching into
/// <c>InSpectra.Gen.Acquisition.Modes.*</c> directly.
/// </summary>
public interface IAcquisitionAnalysisDispatcher
{
    /// <summary>
    /// Runs the analyzer that matches <paramref name="mode"/> against the installed tool
    /// described by <paramref name="target"/>.
    /// </summary>
    /// <param name="target">The materialized CLI target to analyze.</param>
    /// <param name="mode">One of the <see cref="AnalysisMode"/> string constants.</param>
    /// <param name="framework">Optional CLI framework override (used by static mode).</param>
    /// <param name="timeoutSeconds">Per-process timeout for analyzer sub-processes.</param>
    /// <param name="cancellationToken">Cancellation token forwarded to the analyzer.</param>
    Task<AcquisitionAnalysisOutcome> TryAnalyzeAsync(
        CliTargetDescriptor target,
        string mode,
        string? framework,
        int timeoutSeconds,
        CancellationToken cancellationToken);
}
