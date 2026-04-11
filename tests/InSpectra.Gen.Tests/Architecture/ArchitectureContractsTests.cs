using System.Text.RegularExpressions;

namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rule: the <c>Contracts/</c> layer of the Acquisition module must not depend
/// on any <c>Tooling/</c> namespace. Contracts are pure DTOs and interfaces shared
/// across every mode and the app shell; a reverse dependency onto tooling internals
/// would push implementation concerns into the public module surface
/// (see docs/architecture/ARCHITECTURE.md, section "Intra-module dependency rules").
/// </summary>
public sealed class ArchitectureContractsTests
{
    /// <summary>Absolute path to <c>src/InSpectra.Gen.Acquisition/Contracts</c>.</summary>
    private static readonly string ContractsRoot = Path.Combine(
        ArchitecturePolicyScanner.SrcRoot,
        ArchitecturePolicyScanner.AcquisitionProjectName,
        "Contracts");

    /// <summary>
    /// Matches any textual reference to the <c>InSpectra.Gen.Acquisition.Tooling</c>
    /// namespace anywhere in the file, including <c>using</c> directives,
    /// fully-qualified <c>global::</c> identifiers, and XML doc comments. Word boundaries
    /// on both sides prevent false positives on longer namespace suffixes.
    /// </summary>
    private static readonly Regex ToolingNamespaceReference = new(
        @"\bInSpectra\.Gen\.Acquisition\.Tooling\b",
        RegexOptions.Compiled);

    [Fact]
    public void No_contracts_depends_on_tooling()
    {
        Assert.True(Directory.Exists(ContractsRoot), $"Expected Contracts root at '{ContractsRoot}' to exist.");

        var violations = new List<string>();

        foreach (var filePath in Directory.EnumerateFiles(ContractsRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsIgnoredPath(filePath))
            {
                continue;
            }

            var text = File.ReadAllText(filePath);
            if (!ToolingNamespaceReference.IsMatch(text))
            {
                continue;
            }

            var relativePath = ArchitecturePolicyScanner.GetRelativeRepoPath(filePath);
            var lines = text.Split('\n');
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                if (ToolingNamespaceReference.IsMatch(lines[lineIndex]))
                {
                    violations.Add(
                        $"- {relativePath}:{lineIndex + 1} references "
                        + $"'InSpectra.Gen.Acquisition.Tooling' (any reference is forbidden: "
                        + $"using directives, global:: qualifiers, and doc comments all count)");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : "Contracts/ must not depend on any Tooling/ namespace, but found:"
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
