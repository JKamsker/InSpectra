using System.Text.RegularExpressions;

namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rule: "Non-test <c>InternalsVisibleTo</c> is not part of the target architecture."
/// (docs/architecture/ARCHITECTURE.md, "Dependency charter").
/// </summary>
public sealed class ArchitectureInternalsVisibleToTests
{
    /// <summary>
    /// Captures the assembly-name argument of
    /// <c>[assembly: InternalsVisibleTo("SomeAssembly")]</c>. Works whether the attribute
    /// sits in an <c>AssemblyInfo.cs</c> or inline in any other source file.
    /// </summary>
    private static readonly Regex InternalsVisibleToPattern = new(
        @"\[assembly\s*:\s*InternalsVisibleTo\s*\(\s*""(?<target>[^""]+)""",
        RegexOptions.Compiled);

    /// <summary>Non-test assemblies must not appear as targets. Test assemblies end with this.</summary>
    private const string TestAssemblySuffix = ".Tests";

    [Fact]
    public void No_non_test_InternalsVisibleTo()
    {
        var projects = ArchitecturePolicyScanner.EnumerateBackendProjects();
        Assert.NotEmpty(projects);

        var violations = new List<string>();
        var filesScanned = 0;

        foreach (var project in projects)
        {
            foreach (var filePath in ArchitecturePolicyScanner.EnumerateProjectCodeFiles(project))
            {
                filesScanned++;
                ScanFileForViolations(filePath, violations);
            }
        }

        Assert.True(
            filesScanned > 0,
            "Expected InternalsVisibleTo scan to examine at least one tracked .cs file but found none.");

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : "Non-test InternalsVisibleTo targets are forbidden, but found:"
                  + Environment.NewLine
                  + string.Join(Environment.NewLine, violations));
    }

    private static void ScanFileForViolations(string filePath, List<string> violations)
    {
        var text = File.ReadAllText(filePath);
        foreach (Match match in InternalsVisibleToPattern.Matches(text))
        {
            var target = match.Groups["target"].Value;
            if (!target.EndsWith(TestAssemblySuffix, StringComparison.Ordinal))
            {
                violations.Add(
                    $"- {ArchitecturePolicyScanner.GetRelativeRepoPath(filePath)} exposes internals"
                    + $" to '{target}' (only assemblies ending in '{TestAssemblySuffix}' are allowed)");
            }
        }
    }
}
