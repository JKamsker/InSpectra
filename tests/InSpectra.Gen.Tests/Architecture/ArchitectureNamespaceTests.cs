using System.Text.RegularExpressions;

namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rule: "Namespaces must follow the owning project name plus the relative folder
/// path. Folder moves require matching namespace moves."
/// (docs/architecture/ARCHITECTURE.md, "Naming rules").
///
/// Scans every non-generated <c>*.cs</c> under each backend project, reads the first
/// <c>namespace</c> declaration, and compares against <c>{ProjectName}.{RelativeFolder}</c>
/// with directory separators replaced by dots.
/// </summary>
public sealed class ArchitectureNamespaceTests
{
    /// <summary>Matches file-scoped (<c>namespace X;</c>) or block (<c>namespace X {</c>) declarations.</summary>
    private static readonly Regex NamespaceDeclaration = new(
        @"^\s*namespace\s+(?<ns>[A-Za-z_][A-Za-z0-9_\.]*)\s*[;{]",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Files that are intentionally namespace-less or whose namespace is fixed by a
    /// runtime contract rather than folder layout. These are allowlisted and skipped.
    /// </summary>
    private static readonly IReadOnlySet<string> AllowlistedFileNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "GlobalUsings.cs",
        "Program.cs",
    };

    /// <summary>
    /// Path-specific namespace exceptions. Keep this narrow so unrelated files with the
    /// same name do not silently bypass the namespace policy.
    /// </summary>
    private static readonly IReadOnlySet<string> AllowlistedRelativePaths = new HashSet<string>(StringComparer.Ordinal)
    {
        "src/InSpectra.Gen.StartupHook/StartupHook.cs",
    };

    [Fact]
    public void Namespace_matches_folder_path_for_tracked_code_files()
    {
        var projects = ArchitecturePolicyScanner.EnumerateBackendProjects();
        Assert.NotEmpty(projects);

        var violations = new List<string>();
        var checkedCount = 0;

        foreach (var project in projects)
        {
            foreach (var filePath in ArchitecturePolicyScanner.EnumerateProjectCodeFiles(project))
            {
                if (ShouldSkipFile(filePath))
                {
                    continue;
                }

                checkedCount++;
                var expected = ComputeExpectedNamespace(project, filePath);
                var actual = ReadDeclaredNamespace(filePath);

                if (actual is null)
                {
                    violations.Add(
                        $"- {ArchitecturePolicyScanner.GetRelativeRepoPath(filePath)}"
                        + " has no namespace declaration but is not allowlisted");
                    continue;
                }

                if (!string.Equals(actual, expected, StringComparison.Ordinal))
                {
                    violations.Add(
                        $"- {ArchitecturePolicyScanner.GetRelativeRepoPath(filePath)}"
                        + $" declared '{actual}' but folder path expects '{expected}'");
                }
            }
        }

        Assert.True(
            checkedCount > 0,
            "Expected at least one tracked .cs file to namespace-check, but none were found.");

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : $"Namespaces must match folder paths (checked {checkedCount} files):"
                  + Environment.NewLine
                  + string.Join(Environment.NewLine, violations));
    }

    private static bool ShouldSkipFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (AllowlistedFileNames.Contains(fileName))
        {
            return true;
        }

        var relativePath = ArchitecturePolicyScanner.GetRelativeRepoPath(filePath);
        if (AllowlistedRelativePaths.Contains(relativePath))
        {
            return true;
        }

        var segments = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (string.Equals(segments[i], "Properties", StringComparison.Ordinal)
                && string.Equals(segments[i + 1], "AssemblyInfo.cs", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Expected namespace = <c>{ProjectName}</c> joined with the relative folder segments
    /// from the project root to the file, using '.' as the separator. Files directly in the
    /// project root return just the project name.
    /// </summary>
    private static string ComputeExpectedNamespace(CsProject project, string filePath)
    {
        var relativeDir = Path.GetRelativePath(project.Directory, Path.GetDirectoryName(filePath)!);
        if (string.IsNullOrEmpty(relativeDir) || relativeDir == ".")
        {
            return project.Name;
        }

        var folderPart = relativeDir
            .Replace(Path.DirectorySeparatorChar, '.')
            .Replace(Path.AltDirectorySeparatorChar, '.');

        return $"{project.Name}.{folderPart}";
    }

    /// <summary>
    /// Reads the file and returns the first namespace name found, or null if none exists.
    /// Supports both file-scoped and block namespace declarations.
    /// </summary>
    private static string? ReadDeclaredNamespace(string filePath)
    {
        var text = File.ReadAllText(filePath);
        var match = NamespaceDeclaration.Match(text);
        return match.Success ? match.Groups["ns"].Value : null;
    }
}
