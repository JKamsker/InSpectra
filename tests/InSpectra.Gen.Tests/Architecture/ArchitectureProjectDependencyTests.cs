namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rule: project dependency direction must match
/// <c>docs/architecture/ARCHITECTURE.md</c>. This suite parses each <c>.csproj</c> under
/// <c>src/</c> and verifies allowed <c>&lt;ProjectReference&gt;</c> edges.
///
/// The active rule in Phase 1 is the subset the tree already satisfies (see Task.md line 589):
/// <list type="bullet">
///   <item><c>InSpectra.Gen</c> may reference <c>InSpectra.Gen.Acquisition</c> and
///         <c>InSpectra.Gen.StartupHook</c>.</item>
///   <item><c>InSpectra.Gen.Acquisition</c> must have zero project references.</item>
///   <item><c>InSpectra.Gen.StartupHook</c> must have zero project references.</item>
/// </list>
/// </summary>
public sealed class ArchitectureProjectDependencyTests
{
    /// <summary>
    /// Maps each backend project to the set of project references it is allowed to declare.
    /// Anything not listed here is a violation.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedReferences =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [ArchitecturePolicyScanner.AppShellProjectName] = new HashSet<string>(StringComparer.Ordinal)
            {
                ArchitecturePolicyScanner.AcquisitionProjectName,
                ArchitecturePolicyScanner.StartupHookProjectName,
            },
            [ArchitecturePolicyScanner.AcquisitionProjectName] = new HashSet<string>(StringComparer.Ordinal),
            [ArchitecturePolicyScanner.StartupHookProjectName] = new HashSet<string>(StringComparer.Ordinal),
        };

    [Fact]
    public void Project_dependency_direction_follows_charter()
    {
        var projects = ArchitecturePolicyScanner.EnumerateBackendProjects();

        var tracked = projects
            .Where(project => AllowedReferences.ContainsKey(project.Name))
            .ToList();

        Assert.True(
            tracked.Count == AllowedReferences.Count,
            $"Expected to find all charter-tracked projects ({string.Join(", ", AllowedReferences.Keys)})"
            + $" but found only {string.Join(", ", tracked.Select(p => p.Name))}.");

        var violations = new List<string>();
        foreach (var project in tracked)
        {
            var allowed = AllowedReferences[project.Name];
            var actual = ArchitecturePolicyScanner.GetProjectReferences(project);

            foreach (var reference in actual)
            {
                if (!allowed.Contains(reference))
                {
                    var relativeCsProj = ArchitecturePolicyScanner.GetRelativeRepoPath(project.CsProjPath);
                    violations.Add(
                        $"- {relativeCsProj} references '{reference}' but allowed set is "
                        + (allowed.Count == 0 ? "(none)" : $"[{string.Join(", ", allowed)}]"));
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : "Project dependency direction must follow the charter, but found disallowed edges:"
                  + Environment.NewLine
                  + string.Join(Environment.NewLine, violations));
    }
}
