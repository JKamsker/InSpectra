using System.Text.RegularExpressions;

namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rule: "The app shell must stay thin... <c>InSpectra.Gen</c> must not depend on
/// concrete acquisition internals" and "For the initial acquisition-namespace rule,
/// <c>InSpectra.Gen</c> may reference only <c>InSpectra.Gen.Acquisition.Composition</c>,
/// <c>InSpectra.Gen.Acquisition.Contracts</c>, and intentionally exposed public service
/// interfaces" (docs/architecture/ARCHITECTURE.md).
///
/// Currently skipped: <c>src/InSpectra.Gen/Composition/ServiceCollectionExtensions.cs</c>
/// still reaches into <c>InSpectra.Gen.Acquisition.Analysis.*</c>,
/// <c>InSpectra.Gen.Acquisition.Runtime</c>, etc. (Task.md line 570). Step 11 lands the
/// public composition entry points and removes the deep <c>using</c> statements.
/// </summary>
public sealed class ArchitectureAppShellTests
{
    /// <summary>
    /// Only these Acquisition namespaces may appear in <c>using</c> directives inside
    /// the <c>InSpectra.Gen</c> project. The rule uses prefix match so that
    /// <c>InSpectra.Gen.Acquisition.Contracts.Foo</c> is allowed when
    /// <c>InSpectra.Gen.Acquisition.Contracts</c> is in this set.
    /// </summary>
    private static readonly IReadOnlyList<string> AllowedAcquisitionNamespacePrefixes = new[]
    {
        "InSpectra.Gen.Acquisition.Composition",
        "InSpectra.Gen.Acquisition.Contracts",
    };

    /// <summary>Matches <c>using InSpectra.Gen.Acquisition.X.Y;</c> at the top of a file.</summary>
    private static readonly Regex AcquisitionUsingDirective = new(
        @"^\s*using\s+(?<ns>InSpectra\.Gen\.Acquisition(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact(Skip = "Unskip after Step 11 — requires public composition entry points in Acquisition.")]
    public void App_shell_does_not_reference_deep_acquisition_internals()
    {
        var projects = ArchitecturePolicyScanner.EnumerateBackendProjects();
        var appShell = projects.SingleOrDefault(p => p.Name == ArchitecturePolicyScanner.AppShellProjectName);
        Assert.NotNull(appShell);

        var violations = new List<string>();

        foreach (var filePath in ArchitecturePolicyScanner.EnumerateProjectCodeFiles(appShell!))
        {
            var text = File.ReadAllText(filePath);
            foreach (Match match in AcquisitionUsingDirective.Matches(text))
            {
                var ns = match.Groups["ns"].Value;
                if (!IsAllowedNamespace(ns))
                {
                    violations.Add(
                        $"- {ArchitecturePolicyScanner.GetRelativeRepoPath(filePath)} imports '{ns}'"
                        + $" (allowed prefixes: {string.Join(", ", AllowedAcquisitionNamespacePrefixes)})");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : "App shell must not reach into deep Acquisition internals, but found:"
                  + Environment.NewLine
                  + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Returns true when <paramref name="ns"/> equals or starts-with-dot one of the
    /// allowed namespace prefixes. The dot check avoids false positives like
    /// <c>InSpectra.Gen.Acquisition.ContractsShadow</c> matching <c>Contracts</c>.
    /// </summary>
    private static bool IsAllowedNamespace(string ns)
    {
        foreach (var prefix in AllowedAcquisitionNamespacePrefixes)
        {
            if (ns.Equals(prefix, StringComparison.Ordinal))
            {
                return true;
            }

            if (ns.StartsWith(prefix + ".", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
