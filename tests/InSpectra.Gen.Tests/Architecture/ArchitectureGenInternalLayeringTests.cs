using System.Text.RegularExpressions;

namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rules for the thin shell and engine layering:
///
/// <list type="bullet">
///   <item>The shell keeps command parsing and output policy separate.</item>
///   <item><c>OpenCli/</c> stays independent from rendering and use-case orchestration.</item>
///   <item><c>Rendering/</c> stays independent from use-case orchestration.</item>
///   <item><c>Execution/</c> and <c>Targets/</c> stay below modes/rendering rather than
///         growing back upward dependencies.</item>
/// </list>
///
/// The checks below intentionally cover both the shell project and the engine project
/// so the post-rename tree cannot go green by deleting or moving the old roots.
/// </summary>
public sealed class ArchitectureGenInternalLayeringTests
{
    private static readonly string ShellProjectRoot = Path.Combine(
        ArchitecturePolicyScanner.SrcRoot,
        ArchitecturePolicyScanner.AppShellProjectName);

    private static readonly string EngineProjectRoot = Path.Combine(
        ArchitecturePolicyScanner.SrcRoot,
        ArchitecturePolicyScanner.EngineProjectName);

    [Fact]
    public void Shell_output_does_not_depend_on_commands()
        => AssertNoUpstreamImport(
            ShellProjectRoot,
            Path.Combine("Cli", "Output"),
            forbiddenPrefixes: ["InSpectra.Gen.Commands"]);

    [Fact]
    public void Engine_opencli_does_not_depend_on_rendering_or_use_cases()
        => AssertNoUpstreamImport(
            EngineProjectRoot,
            "OpenCli",
            forbiddenPrefixes:
            [
                "InSpectra.Lib.Rendering",
                "InSpectra.Lib.UseCases",
            ]);

    [Fact]
    public void Engine_rendering_does_not_depend_on_use_cases()
        => AssertNoUpstreamImport(
            EngineProjectRoot,
            "Rendering",
            forbiddenPrefixes: ["InSpectra.Lib.UseCases"]);

    [Fact]
    public void Engine_execution_does_not_depend_on_modes_rendering_or_use_cases()
        => AssertNoUpstreamImport(
            EngineProjectRoot,
            "Execution",
            forbiddenPrefixes:
            [
                "InSpectra.Lib.Modes",
                "InSpectra.Lib.Rendering",
                "InSpectra.Lib.UseCases",
            ]);

    [Fact]
    public void Engine_targets_does_not_depend_on_modes_or_rendering()
        => AssertNoUpstreamImport(
            EngineProjectRoot,
            "Targets",
            forbiddenPrefixes:
            [
                "InSpectra.Lib.Modes",
                "InSpectra.Lib.Rendering",
            ]);

    private static void AssertNoUpstreamImport(
        string projectRoot,
        string subtree,
        IReadOnlyList<string> forbiddenPrefixes)
    {
        var subtreeRoot = Path.Combine(projectRoot, subtree);
        Assert.True(
            Directory.Exists(subtreeRoot),
            $"Expected subtree '{subtree}' at '{subtreeRoot}' to exist.");

        var violations = new List<string>();
        var filesScanned = 0;

        foreach (var filePath in Directory.EnumerateFiles(subtreeRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsIgnoredPath(filePath))
            {
                continue;
            }

            filesScanned++;
            var text = File.ReadAllText(filePath);

            foreach (Match match in UsingDirective.Matches(text))
            {
                var ns = match.Groups["ns"].Value;
                foreach (var prefix in forbiddenPrefixes)
                {
                    if (ns.Equals(prefix, StringComparison.Ordinal)
                        || ns.StartsWith(prefix + ".", StringComparison.Ordinal))
                    {
                        violations.Add(
                            $"- {ArchitecturePolicyScanner.GetRelativeRepoPath(filePath)} imports '{ns}'");
                        break;
                    }
                }
            }
        }

        Assert.True(
            filesScanned > 0,
            $"Expected '{subtreeRoot}' to contain at least one *.cs file but found none.");

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : $"'{subtree}' must not depend on {string.Join(" / ", forbiddenPrefixes)}, but found:"
                  + Environment.NewLine
                  + string.Join(Environment.NewLine, violations));
    }

    private static readonly Regex UsingDirective = new(
        @"^\s*using\s+(?<ns>InSpectra\.Gen(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static bool IsIgnoredPath(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment =>
            string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase));
    }
}
