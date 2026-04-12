using System.ComponentModel;
using System.Diagnostics;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests;

public sealed class RepositoryCodeFilePolicyTests
{
    [Fact]
    public void Source_And_Test_Code_Files_Stay_Within_The_Practical_Line_Limit()
        => AssertNoViolations(
            new CodeFilePolicy(
                MaximumLines: 300,
                Description: "Non-generated C# files under `src` and `tests` should stay below the repo's practical 300-line target.",
                Extensions: [".cs"]));

    [Fact]
    public void Source_And_Test_Code_Files_Stay_Within_The_Hard_Line_Limit()
        => AssertNoViolations(
            new CodeFilePolicy(
                MaximumLines: 500,
                Description: "Non-generated C# and frontend TypeScript files under `src` and `tests` must stay within the hard 500-line limit.",
                Extensions: [".cs", ".ts", ".tsx"]));

    private static void AssertNoViolations(CodeFilePolicy policy)
    {
        var trackedFiles = EnumerateCodeFiles(policy.Extensions).ToArray();
        Assert.True(
            trackedFiles.Length > 0,
            $"Expected to scan at least one non-generated code file under 'src' or 'tests' with extensions {string.Join(", ", policy.Extensions)}, but found none.");

        var violations = trackedFiles
            .Select(path => new CodeFileLength(
                RelativePath: Path.GetRelativePath(FixturePaths.RepoRoot, path).Replace('\\', '/'),
                LineCount: File.ReadLines(path).Count()))
            .Where(file => file.LineCount > policy.MaximumLines)
            .OrderByDescending(file => file.LineCount)
            .ThenBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            violations.Length == 0
                ? null
                : $"{policy.Description}{Environment.NewLine}{string.Join(Environment.NewLine, violations.Select(static file => $"- {file.RelativePath} ({file.LineCount} lines)"))}");
    }

    private static IEnumerable<string> EnumerateCodeFiles(IReadOnlyCollection<string> extensions)
    {
        var extensionSet = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
        var yieldedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var absolutePath in EnumerateTrackedCodeFiles(extensionSet))
        {
            if (yieldedPaths.Add(absolutePath))
            {
                yield return absolutePath;
            }
        }

        foreach (var absolutePath in EnumerateWorkingTreeCodeFiles(extensionSet))
        {
            if (yieldedPaths.Add(absolutePath))
            {
                yield return absolutePath;
            }
        }
    }

    private static IEnumerable<string> EnumerateTrackedCodeFiles(HashSet<string> extensionSet)
    {
        if (!TryEnumerateTrackedRepoPaths(out var trackedPaths))
        {
            yield break;
        }

        foreach (var relativePath in trackedPaths)
        {
            if (!extensionSet.Contains(Path.GetExtension(relativePath)))
            {
                continue;
            }

            var absolutePath = Path.Combine(FixturePaths.RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absolutePath) && !IsGeneratedFile(absolutePath))
            {
                yield return absolutePath;
            }
        }
    }

    private static IEnumerable<string> EnumerateWorkingTreeCodeFiles(HashSet<string> extensionSet)
    {
        foreach (var root in new[] { "src", "tests" })
        {
            var rootPath = Path.Combine(FixturePaths.RepoRoot, root);
            if (!Directory.Exists(rootPath))
            {
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
            {
                if (!extensionSet.Contains(Path.GetExtension(path))
                    || IsIgnoredPath(path)
                    || IsGeneratedFile(path))
                {
                    continue;
                }

                yield return path;
            }
        }
    }

    private static bool TryEnumerateTrackedRepoPaths(out string[] trackedPaths)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = FixturePaths.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add("ls-files");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("src");
        startInfo.ArgumentList.Add("tests");

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                trackedPaths = [];
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            _ = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                trackedPaths = [];
                return false;
            }

            trackedPaths = output
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            trackedPaths = [];
            return false;
        }
    }

    private static bool IsIgnoredPath(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment =>
            string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase)
            || string.Equals(segment, "node_modules", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsGeneratedFile(string path)
    {
        var fileName = Path.GetFileName(path);
        if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".generated.ts", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".generated.tsx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var line in File.ReadLines(path).Take(5))
        {
            if (line.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase)
                || line.Contains("@generated", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record CodeFilePolicy(int MaximumLines, string Description, IReadOnlyCollection<string> Extensions);

    private sealed record CodeFileLength(string RelativePath, int LineCount);
}
