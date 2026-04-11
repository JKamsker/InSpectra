using System.Text.RegularExpressions;

namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rule: the <c>Tooling/</c> layer of the Acquisition module must not depend
/// on any acquisition mode implementation. Tooling is a shared infrastructure layer
/// used by every mode, and depending on a specific mode from Tooling creates an
/// implicit bidirectional coupling (see docs/architecture/ARCHITECTURE.md, section
/// "Intra-module dependency rules").
/// </summary>
public sealed class ArchitectureToolingTests
{
    /// <summary>Absolute path to <c>src/InSpectra.Gen.Acquisition/Tooling</c>.</summary>
    private static readonly string ToolingRoot = Path.Combine(
        ArchitecturePolicyScanner.SrcRoot,
        ArchitecturePolicyScanner.AcquisitionProjectName,
        "Tooling");

    /// <summary>
    /// Matches <c>using InSpectra.Gen.Acquisition.Modes.&lt;ModeName&gt;</c> at any depth.
    /// Captures the first segment after <c>Modes.</c> so failure messages name the target.
    /// </summary>
    private static readonly Regex ModeUsingDirective = new(
        @"^\s*using\s+InSpectra\.Gen\.Acquisition\.Modes\.(?<mode>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact]
    public void No_tooling_depends_on_modes()
    {
        Assert.True(Directory.Exists(ToolingRoot), $"Expected Tooling root at '{ToolingRoot}' to exist.");

        var violations = new List<string>();

        foreach (var filePath in Directory.EnumerateFiles(ToolingRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsIgnoredPath(filePath))
            {
                continue;
            }

            var text = File.ReadAllText(filePath);
            foreach (Match match in ModeUsingDirective.Matches(text))
            {
                var referencedMode = match.Groups["mode"].Value;
                violations.Add(
                    $"- {ArchitecturePolicyScanner.GetRelativeRepoPath(filePath)}"
                    + $" imports 'InSpectra.Gen.Acquisition.Modes.{referencedMode}'");
            }
        }

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : "Tooling/ must not depend on any Modes/<Mode>/ namespace, but found:"
                  + Environment.NewLine
                  + string.Join(Environment.NewLine, violations));
    }

    private static bool IsIgnoredPath(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment =>
            string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase));
    }
}
