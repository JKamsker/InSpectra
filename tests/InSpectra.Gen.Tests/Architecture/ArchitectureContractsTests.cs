using System.Text.RegularExpressions;

namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rule: the <c>Contracts/</c> layer of the engine must not depend
/// on any <c>Tooling/</c> namespace. Contracts are pure DTOs and interfaces shared
/// across every mode and the app shell; a reverse dependency onto tooling internals
/// would push implementation concerns into the public module surface
/// (see docs/architecture/ARCHITECTURE.md, section "Intra-module dependency rules").
/// </summary>
public sealed class ArchitectureContractsTests
{
    /// <summary>Absolute path to <c>src/InSpectra.Gen.Engine/Contracts</c>.</summary>
    private static readonly string ContractsRoot = Path.Combine(
        ArchitecturePolicyScanner.SrcRoot,
        ArchitecturePolicyScanner.EngineProjectName,
        "Contracts");

    /// <summary>
    /// Matches any textual reference to the <c>InSpectra.Gen.Engine.Tooling</c>
    /// namespace anywhere in the file, including <c>using</c> directives,
    /// fully-qualified <c>global::</c> identifiers, and XML doc comments. Word boundaries
    /// on both sides prevent false positives on longer namespace suffixes.
    /// </summary>
    private static readonly Regex ToolingNamespaceReference = new(
        @"\bInSpectra\.Gen\.Engine\.Tooling\b",
        RegexOptions.Compiled);

    /// <summary>
    /// Positive anchor for the Contracts scan: at least one file under <c>Contracts/</c>
    /// should declare a Contracts namespace, otherwise the test likely scanned the wrong
    /// surface and would pass vacuously.
    /// </summary>
    private static readonly Regex ContractsNamespaceDeclaration = new(
        @"^\s*namespace\s+InSpectra\.Gen\.Engine\.Contracts(?:\.[A-Za-z_][A-Za-z0-9_]*)*\s*[;{]",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact]
    public void No_contracts_depends_on_tooling()
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
                        + $"'InSpectra.Gen.Engine.Tooling' (any reference is forbidden: "
                        + $"using directives, global:: qualifiers, and doc comments all count)");
                }
            }
        }

        Assert.True(
            filesScanned > 0,
            $"Expected Contracts root at '{ContractsRoot}' to contain at least one tracked .cs file but found none.");

        Assert.True(
            contractsNamespacesSeen > 0,
            $"Expected Contracts scan under '{ContractsRoot}' to encounter at least one InSpectra.Gen.Engine.Contracts namespace declaration but found none.");

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
