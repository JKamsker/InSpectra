namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rule: "No new top-level <c>Runtime</c>, <c>Infrastructure</c>, <c>Models</c>,
/// <c>Support</c>, <c>Helpers</c>, or <c>Misc</c> roots."
/// (docs/architecture/ARCHITECTURE.md, "Naming rules").
/// </summary>
public sealed class ArchitectureForbiddenBucketsTests
{
    /// <summary>
    /// Top-level folder names that are not allowed at the root of any backend project.
    /// "Top-level" means a direct child of <c>src/&lt;Project&gt;/</c>.
    /// </summary>
    private static readonly IReadOnlySet<string> ForbiddenTopLevelFolders =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Runtime",
            "Infrastructure",
            "Models",
            "Support",
            "Helpers",
            "Misc",
        };

    [Fact]
    public void No_forbidden_top_level_buckets()
    {
        var projects = ArchitecturePolicyScanner.EnumerateBackendProjects();
        var violations = new List<string>();

        foreach (var project in projects)
        {
            foreach (var directory in Directory.EnumerateDirectories(project.Directory))
            {
                var name = Path.GetFileName(directory);
                if (ForbiddenTopLevelFolders.Contains(name))
                {
                    violations.Add($"- {ArchitecturePolicyScanner.GetRelativeRepoPath(directory)}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : "Expected no forbidden top-level buckets, but found:"
                  + Environment.NewLine
                  + string.Join(Environment.NewLine, violations));
    }
}
