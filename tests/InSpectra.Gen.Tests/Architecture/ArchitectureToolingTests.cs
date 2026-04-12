using System.Text.RegularExpressions;

namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rule: the <c>Tooling/</c> layer of the engine must not depend
/// on any mode implementation. Tooling is a shared infrastructure layer
/// used by every mode, and depending on a specific mode from Tooling creates an
/// implicit bidirectional coupling (see docs/architecture/ARCHITECTURE.md, section
/// "Intra-module dependency rules").
/// </summary>
public sealed class ArchitectureToolingTests
{
    /// <summary>Absolute path to <c>src/InSpectra.Lib/Tooling</c>.</summary>
    private static readonly string ToolingRoot = Path.Combine(
        ArchitecturePolicyScanner.SrcRoot,
        ArchitecturePolicyScanner.EngineProjectName,
        "Tooling");

    /// <summary>
    /// Matches <c>using InSpectra.Lib.Modes.&lt;ModeName&gt;</c> at any depth.
    /// Captures the first segment after <c>Modes.</c> so failure messages name the target.
    /// </summary>
    private static readonly Regex ModeUsingDirective = new(
        @"^\s*using\s+InSpectra\.Lib\.Modes\.(?<mode>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Positive anchor for the Tooling scan: at least one file under <c>Tooling/</c>
    /// should declare a Tooling namespace, otherwise the test likely scanned the wrong
    /// surface and would pass vacuously.
    /// </summary>
    private static readonly Regex ToolingNamespaceDeclaration = new(
        @"^\s*namespace\s+InSpectra\.Lib\.Tooling(?:\.[A-Za-z_][A-Za-z0-9_]*)*\s*[;{]",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact]
    public void No_tooling_depends_on_modes()
    {
        Assert.True(Directory.Exists(ToolingRoot), $"Expected Tooling root at '{ToolingRoot}' to exist.");

        var violations = new List<string>();
        var filesScanned = 0;
        var toolingNamespacesSeen = 0;

        foreach (var filePath in Directory.EnumerateFiles(ToolingRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsIgnoredPath(filePath))
            {
                continue;
            }

            filesScanned++;
            var text = File.ReadAllText(filePath);
            if (ToolingNamespaceDeclaration.IsMatch(text))
            {
                toolingNamespacesSeen++;
            }

            foreach (Match match in ModeUsingDirective.Matches(text))
            {
                var referencedMode = match.Groups["mode"].Value;
                violations.Add(
                    $"- {ArchitecturePolicyScanner.GetRelativeRepoPath(filePath)}"
                    + $" imports 'InSpectra.Lib.Modes.{referencedMode}'");
            }
        }

        Assert.True(
            filesScanned > 0,
            $"Expected Tooling root at '{ToolingRoot}' to contain at least one tracked .cs file but found none.");

        Assert.True(
            toolingNamespacesSeen > 0,
            $"Expected Tooling scan under '{ToolingRoot}' to encounter at least one InSpectra.Lib.Tooling namespace declaration but found none.");

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
