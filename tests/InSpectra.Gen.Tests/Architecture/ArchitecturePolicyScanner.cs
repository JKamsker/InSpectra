using System.Xml.Linq;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Shared helpers for architecture policy tests: locate the backend C# projects,
/// enumerate their non-generated source files, and parse <c>ProjectReference</c> elements.
/// Mirrors the xUnit + <see cref="FixturePaths"/> pattern used by
/// <c>RepositoryCodeFilePolicyTests</c>.
/// </summary>
internal static class ArchitecturePolicyScanner
{
    /// <summary>Name of the backend C# project that hosts the app shell.</summary>
    public const string AppShellProjectName = "InSpectra.Gen";

    /// <summary>Name of the engine module project.</summary>
    public const string EngineProjectName = "InSpectra.Lib";

    /// <summary>Name of the discovery orchestration tool project.</summary>
    public const string DiscoveryToolProjectName = "InSpectra.Discovery.Tool";

    /// <summary>Name of the startup-hook project.</summary>
    public const string StartupHookProjectName = "InSpectra.Gen.StartupHook";

    /// <summary>Name of the cross-module foundational primitives project.</summary>
    public const string CoreProjectName = "InSpectra.Lib";

    /// <summary>Absolute path to <c>src/</c> in the repo.</summary>
    public static string SrcRoot { get; } = Path.Combine(FixturePaths.RepoRoot, "src");

    /// <summary>
    /// Returns all backend C# projects that own the architecture charter:
    /// <c>InSpectra.Gen</c>, <c>InSpectra.Lib</c>, <c>InSpectra.Discovery.Tool</c>,
    /// and <c>InSpectra.Gen.StartupHook</c>. <c>InSpectra.UI</c> is a
    /// Vite/TypeScript frontend and is intentionally excluded.
    /// </summary>
    public static IReadOnlyList<CsProject> EnumerateBackendProjects()
    {
        if (!Directory.Exists(SrcRoot))
        {
            throw new InvalidOperationException(
                $"Expected src directory at '{SrcRoot}' but it was not found.");
        }

        var results = new List<CsProject>();
        foreach (var csprojPath in Directory.EnumerateFiles(SrcRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var projectDirectory = Path.GetDirectoryName(csprojPath)!;
            var projectName = Path.GetFileNameWithoutExtension(csprojPath);
            results.Add(new CsProject(projectName, projectDirectory, csprojPath));
        }

        results.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));
        return results;
    }

    /// <summary>
    /// Returns the set of <c>&lt;ProjectReference Include="..."/&gt;</c> target project names
    /// for the given .csproj. Project names are the file name without extension,
    /// so references like <c>..\InSpectra.Lib\InSpectra.Lib.csproj</c>
    /// produce <c>InSpectra.Lib</c>. Missing files throw.
    /// </summary>
    public static IReadOnlyList<string> GetProjectReferences(CsProject project)
    {
        var document = XDocument.Load(project.CsProjPath);
        var references = new List<string>();
        foreach (var element in document.Descendants("ProjectReference"))
        {
            var include = element.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include))
            {
                continue;
            }

            var referencedName = Path.GetFileNameWithoutExtension(include.Replace('\\', '/'));
            references.Add(referencedName);
        }

        references.Sort(StringComparer.Ordinal);
        return references;
    }

    /// <summary>
    /// Enumerates non-generated <c>*.cs</c> files for the given project. Skips <c>bin/</c>,
    /// <c>obj/</c>, <c>.g.cs</c>, <c>.generated.cs</c>, <c>.designer.cs</c> files, and files
    /// whose first five lines contain <c>&lt;auto-generated</c>. This mirrors
    /// <c>RepositoryCodeFilePolicyTests</c> so both policy suites agree on "tracked source".
    /// </summary>
    public static IEnumerable<string> EnumerateProjectCodeFiles(CsProject project)
    {
        foreach (var path in Directory.EnumerateFiles(project.Directory, "*.cs", SearchOption.AllDirectories))
        {
            if (IsIgnoredPath(path) || IsGeneratedFile(path))
            {
                continue;
            }

            yield return path;
        }
    }

    /// <summary>
    /// Computes the repo-relative POSIX-style path for a file, anchored at
    /// <see cref="FixturePaths.RepoRoot"/>. Used to produce stable failure messages
    /// that look identical on Windows and Linux CI.
    /// </summary>
    public static string GetRelativeRepoPath(string absolutePath) =>
        Path.GetRelativePath(FixturePaths.RepoRoot, absolutePath).Replace('\\', '/');

    private static bool IsIgnoredPath(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment =>
            string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsGeneratedFile(string path)
    {
        var fileName = Path.GetFileName(path);
        if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var line in File.ReadLines(path).Take(5))
        {
            if (line.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Describes one C# project under <c>src/</c>: assembly/project name, containing directory,
/// and absolute path to the .csproj file.
/// </summary>
internal sealed record CsProject(string Name, string Directory, string CsProjPath);
