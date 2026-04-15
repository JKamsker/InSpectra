namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rule: project dependency direction must match
/// <c>docs/architecture/ARCHITECTURE.md</c>. This suite parses each <c>.csproj</c> under
/// <c>src/</c> and verifies allowed <c>&lt;ProjectReference&gt;</c> edges.
///
/// Active rule set:
/// <list type="bullet">
///   <item><c>InSpectra.Gen</c> may reference <c>InSpectra.Lib</c> and
///         <c>InSpectra.Gen.StartupHook</c>.</item>
///   <item><c>InSpectra.Discovery.Tool</c> may reference <c>InSpectra.Lib</c> and
///         <c>InSpectra.Gen.StartupHook</c>.</item>
///   <item><c>InSpectra.Lib</c> may reference <c>InSpectra.Gen.StartupHook</c>
///         (build-order only, for NuGet DLL embedding).</item>
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
                ArchitecturePolicyScanner.EngineProjectName,
                ArchitecturePolicyScanner.StartupHookProjectName,
                ArchitecturePolicyScanner.CoreProjectName,
            },
            [ArchitecturePolicyScanner.DiscoveryToolProjectName] = new HashSet<string>(StringComparer.Ordinal)
            {
                ArchitecturePolicyScanner.EngineProjectName,
                ArchitecturePolicyScanner.StartupHookProjectName,
            },
            [ArchitecturePolicyScanner.EngineProjectName] = new HashSet<string>(StringComparer.Ordinal)
            {
                ArchitecturePolicyScanner.StartupHookProjectName,
            },
            [ArchitecturePolicyScanner.StartupHookProjectName] = new HashSet<string>(StringComparer.Ordinal),
        };

    [Fact]
    public void Project_dependency_direction_follows_charter()
    {
        var projects = ArchitecturePolicyScanner.EnumerateBackendProjects();
        Assert.NotEmpty(projects);

        var expectedProjectNames = AllowedReferences.Keys
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToList();
        var actualProjectNames = projects
            .Select(project => project.Name)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToList();
        var missingProjects = expectedProjectNames
            .Where(expected => !actualProjectNames.Contains(expected, StringComparer.Ordinal))
            .ToList();
        var unexpectedProjects = projects
            .Where(project => !AllowedReferences.ContainsKey(project.Name))
            .Select(project =>
                $"{project.Name} ({ArchitecturePolicyScanner.GetRelativeRepoPath(project.CsProjPath)})")
            .OrderBy(static project => project, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missingProjects.Count == 0 && unexpectedProjects.Count == 0,
            "Expected backend project policy coverage to match the charter exactly."
            + Environment.NewLine
            + $"Expected: [{string.Join(", ", expectedProjectNames)}]"
            + Environment.NewLine
            + $"Actual:   [{string.Join(", ", actualProjectNames)}]"
            + Environment.NewLine
            + (missingProjects.Count == 0
                ? "Missing:  (none)"
                : $"Missing:  [{string.Join(", ", missingProjects)}]")
            + Environment.NewLine
            + (unexpectedProjects.Count == 0
                ? "Unexpected: (none)"
                : $"Unexpected: [{string.Join(", ", unexpectedProjects)}]"));

        var trackedProjects = projects
            .Where(project => AllowedReferences.ContainsKey(project.Name))
            .ToList();

        var violations = new List<string>();
        foreach (var project in trackedProjects)
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
