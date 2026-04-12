using System.Text.RegularExpressions;

namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rule: the app shell must stay thin. <c>InSpectra.Gen</c> may depend only on
/// the engine composition root plus intentionally exposed engine contracts and service
/// interfaces; it must not reach into engine implementation namespaces directly
/// (docs/architecture/ARCHITECTURE.md). Internal app-shell global usings are also
/// forbidden because they hide cross-module imports from the regex-based architecture
/// scanners that enforce those same layering rules.
///
/// The four <c>Cli*Exception</c> types live in <c>InSpectra.Gen.Core</c> so both the app
/// shell and engine can reference them without widening the engine surface.
/// </summary>
public sealed class ArchitectureAppShellTests
{
    /// <summary>
    /// Only these engine namespaces may appear in <c>using</c> directives inside
    /// the <c>InSpectra.Gen</c> project. The rule uses prefix match so that
    /// <c>InSpectra.Gen.Engine.UseCases.Generate.Requests</c> is allowed when
    /// <c>InSpectra.Gen.Engine.UseCases.Generate</c> is in this set.
    /// </summary>
    private static readonly IReadOnlyList<string> AllowedEngineNamespacePrefixes = new[]
    {
        "InSpectra.Gen.Engine.Composition",
        "InSpectra.Gen.Engine.Contracts",
        "InSpectra.Gen.Engine.Rendering.Contracts",
        "InSpectra.Gen.Engine.UseCases.Generate",
    };

    /// <summary>Matches <c>using InSpectra.Gen.Engine.X.Y;</c> at the top of a file.</summary>
    private static readonly Regex EngineUsingDirective = new(
        @"^\s*using\s+(?<ns>InSpectra\.Gen\.Engine(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Matches project-wide internal global usings like
    /// <c>global using InSpectra.Gen.Engine.Rendering.Contracts;</c>. Those make the
    /// downstream layering tests look only at the consumer file body while the real
    /// dependency is smuggled in through a project-root import surface.
    /// </summary>
    private static readonly Regex InternalGlobalUsingDirective = new(
        @"^\s*global\s+using\s+(?<ns>InSpectra\.Gen(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact]
    public void App_shell_does_not_reference_deep_engine_internals()
    {
        var projects = ArchitecturePolicyScanner.EnumerateBackendProjects();
        Assert.NotEmpty(projects);

        var appShell = projects.SingleOrDefault(p => p.Name == ArchitecturePolicyScanner.AppShellProjectName);
        Assert.NotNull(appShell);

        var violations = new List<string>();
        var filesScanned = 0;

        foreach (var filePath in ArchitecturePolicyScanner.EnumerateProjectCodeFiles(appShell!))
        {
            filesScanned++;
            var text = File.ReadAllText(filePath);
            foreach (Match match in EngineUsingDirective.Matches(text))
            {
                var ns = match.Groups["ns"].Value;
                if (!IsAllowedNamespace(ns))
                {
                    violations.Add(
                        $"- {ArchitecturePolicyScanner.GetRelativeRepoPath(filePath)} imports '{ns}'"
                        + $" (allowed prefixes: {string.Join(", ", AllowedEngineNamespacePrefixes)})");
                }
            }
        }

        Assert.True(
            filesScanned > 0,
            $"Expected app shell project '{appShell!.Name}' at '{appShell.Directory}' to contain at least one tracked .cs file but found none.");

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : "App shell must not reach into deep engine internals, but found:"
                  + Environment.NewLine
                  + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void App_shell_does_not_hide_internal_dependencies_behind_global_usings()
    {
        var projects = ArchitecturePolicyScanner.EnumerateBackendProjects();
        Assert.NotEmpty(projects);

        var appShell = projects.SingleOrDefault(p => p.Name == ArchitecturePolicyScanner.AppShellProjectName);
        Assert.NotNull(appShell);

        var violations = new List<string>();
        var filesScanned = 0;

        foreach (var filePath in ArchitecturePolicyScanner.EnumerateProjectCodeFiles(appShell!))
        {
            filesScanned++;
            var text = File.ReadAllText(filePath);
            foreach (Match match in InternalGlobalUsingDirective.Matches(text))
            {
                violations.Add(
                    $"- {ArchitecturePolicyScanner.GetRelativeRepoPath(filePath)} declares global using '{match.Groups["ns"].Value}'");
            }
        }

        Assert.True(
            filesScanned > 0,
            $"Expected app shell project '{appShell!.Name}' at '{appShell.Directory}' to contain at least one tracked .cs file but found none.");

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : "App shell must not hide internal dependencies behind project-wide global usings, but found:"
                  + Environment.NewLine
                  + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Returns true when <paramref name="ns"/> equals or starts-with-dot one of the
    /// allowed namespace prefixes. The dot check avoids false positives like
    /// <c>InSpectra.Gen.Engine.ContractsShadow</c> matching <c>Contracts</c>.
    /// </summary>
    private static bool IsAllowedNamespace(string ns)
    {
        foreach (var prefix in AllowedEngineNamespacePrefixes)
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
