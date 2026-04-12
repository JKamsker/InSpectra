using System.Text.RegularExpressions;

namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rule: the <c>Contracts/</c> layer of the engine must not depend
/// on any <c>Modes/</c> namespace. <c>Contracts/</c> is the foundational layer shared
/// by every mode and the app shell; a reverse dependency onto a specific mode would
/// invert the dependency graph and drag mode-owned types (including dnlib-backed
/// wrappers) into the foundational surface
/// (see docs/architecture/ARCHITECTURE.md, section "Intra-module dependency rules").
///
/// <para>
/// This test mirrors <see cref="ArchitectureContractsTests.No_contracts_depends_on_tooling"/>
/// and exists specifically to guard against the Phase 2A regression where
/// <c>IStaticAttributeReader</c> was briefly promoted into <c>Contracts/Providers/</c>
/// while its signature still referenced <c>StaticCommandDefinition</c> and
/// <c>ScannedModule</c> from <c>Modes.Static.*</c>.
/// </para>
/// </summary>
public sealed class ArchitectureContractsModesTests
{
    /// <summary>Absolute path to <c>src/InSpectra.Lib/Contracts</c>.</summary>
    private static readonly string ContractsRoot = Path.Combine(
        ArchitecturePolicyScanner.SrcRoot,
        ArchitecturePolicyScanner.EngineProjectName,
        "Contracts");

    /// <summary>
    /// Matches any textual reference to the <c>InSpectra.Lib.Modes</c>
    /// namespace anywhere in the file, including <c>using</c> directives,
    /// fully-qualified <c>global::</c> identifiers, and XML doc comments. Word
    /// boundaries on both sides prevent false positives on longer namespace suffixes.
    /// </summary>
    private static readonly Regex ModesNamespaceReference = new(
        @"\bInSpectra\.Lib\.Modes\b",
        RegexOptions.Compiled);

    /// <summary>
    /// Positive anchor for the Contracts scan: at least one file under <c>Contracts/</c>
    /// should declare a Contracts namespace, otherwise the test likely scanned the wrong
    /// surface and would pass vacuously.
    /// </summary>
    private static readonly Regex ContractsNamespaceDeclaration = new(
        @"^\s*namespace\s+InSpectra\.Lib\.Contracts(?:\.[A-Za-z_][A-Za-z0-9_]*)*\s*[;{]",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact]
    public void No_contracts_depends_on_modes()
    {
        Assert.True(Directory.Exists(ContractsRoot), $"Expected Contracts root at '{ContractsRoot}' to exist.");

        var violations = new List<string>();
        var filesScanned = 0;
        var contractsNamespacesSeen = 0;

        foreach (var filePath in Directory.EnumerateFiles(ContractsRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsIgnoredPath(filePath))
            {
                continue;
            }

            filesScanned++;
            var text = File.ReadAllText(filePath);
            if (ContractsNamespaceDeclaration.IsMatch(text))
            {
                contractsNamespacesSeen++;
            }

            if (!ModesNamespaceReference.IsMatch(text))
            {
                continue;
            }

            var relativePath = ArchitecturePolicyScanner.GetRelativeRepoPath(filePath);
            var lines = text.Split('\n');
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                if (ModesNamespaceReference.IsMatch(lines[lineIndex]))
                {
                    violations.Add(
                        $"- {relativePath}:{lineIndex + 1} references "
                        + $"'InSpectra.Lib.Modes' (any reference is forbidden: "
                        + $"using directives, global:: qualifiers, and doc comments all count)");
                }
            }
        }

        Assert.True(
            filesScanned > 0,
            $"Expected Contracts root at '{ContractsRoot}' to contain at least one tracked .cs file but found none.");

        Assert.True(
            contractsNamespacesSeen > 0,
            $"Expected Contracts scan under '{ContractsRoot}' to encounter at least one InSpectra.Lib.Contracts namespace declaration but found none.");

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : "Contracts/ must not depend on any Modes/ namespace, but found:"
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
