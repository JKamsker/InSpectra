using InSpectra.Gen.Runtime;
using Microsoft.Extensions.Options;

namespace InSpectra.Gen.Services;

public sealed class ViewerBundleLocatorOptions
{
    public string? PackagedRootPath { get; init; }

    public string? RepositoryRootPath { get; init; }

    public string? NpmExecutablePath { get; init; }

    public int NpmTimeoutSeconds { get; init; } = 300;
}

public class ViewerBundleLocator(
    ExecutableResolver executableResolver,
    IProcessRunner processRunner,
    IOptions<ViewerBundleLocatorOptions> options)
    : IViewerBundleLocator
{
    private const string FrontendBuildHint = "Run `npm ci` and `npm run build` in `src/InSpectra.UI` to build the viewer bundle.";
    private static readonly string[] FrontendInputFiles =
    [
        "index.html",
        "static.html",
        "package.json",
        "package-lock.json",
        "tsconfig.json",
        "vite.config.ts",
    ];

    public async Task<string> ResolveAsync(CancellationToken cancellationToken)
    {
        var packagedPath = options.Value.PackagedRootPath ?? Path.Combine(AppContext.BaseDirectory, "InSpectra.UI", "dist");
        var repositoryRoot = options.Value.RepositoryRootPath ?? FindRepositoryRoot();
        var frontendRoot = repositoryRoot is null
            ? null
            : Path.Combine(repositoryRoot, "src", "InSpectra.UI");

        if (frontendRoot is not null && HasFrontendProject(frontendRoot))
        {
            return await ResolveRepositoryBundleAsync(frontendRoot, cancellationToken);
        }

        if (HasBundle(packagedPath))
        {
            return packagedPath;
        }

        throw new CliUsageException($"InSpectra.UI bundle could not be located beside the tool. {FrontendBuildHint}");
    }

    protected virtual async Task BuildBundleAsync(string frontendRoot, string repositoryDist, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(frontendRoot))
        {
            throw new CliUsageException($"InSpectra.UI sources were not found at `{frontendRoot}`. {FrontendBuildHint}");
        }

        var packageJsonPath = Path.Combine(frontendRoot, "package.json");
        var packageLockPath = Path.Combine(frontendRoot, "package-lock.json");
        if (!File.Exists(packageJsonPath) || !File.Exists(packageLockPath))
        {
            throw new CliUsageException($"InSpectra.UI package metadata is missing in `{frontendRoot}`. {FrontendBuildHint}");
        }

        string npmExecutable;
        try
        {
            npmExecutable = options.Value.NpmExecutablePath ?? executableResolver.Resolve("npm", frontendRoot);
        }
        catch (CliException)
        {
            throw new CliUsageException($"InSpectra.UI dist was not found and `npm` is not available on PATH. {FrontendBuildHint}");
        }

        try
        {
            switch (GetNodeModulesState(frontendRoot))
            {
                case NodeModulesState.Missing:
                    await processRunner.RunAsync(npmExecutable, frontendRoot, ["ci"], options.Value.NpmTimeoutSeconds, cancellationToken);
                    break;
                case NodeModulesState.Incomplete:
                    await processRunner.RunAsync(npmExecutable, frontendRoot, ["install"], options.Value.NpmTimeoutSeconds, cancellationToken);
                    break;
            }

            await processRunner.RunAsync(npmExecutable, frontendRoot, ["run", "build"], options.Value.NpmTimeoutSeconds, cancellationToken);
        }
        catch (CliException exception)
        {
            throw new CliUsageException(
                $"Failed to build InSpectra.UI in `{frontendRoot}`.",
                [exception.Message, .. exception.Details, $"Expected bundle path: `{repositoryDist}`."]);
        }
    }

    private static string? FindRepositoryRoot()
    {
        foreach (var start in CandidateDirectories())
        {
            var current = new DirectoryInfo(start);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "InSpectra.Gen.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateDirectories()
    {
        yield return AppContext.BaseDirectory;
        yield return Directory.GetCurrentDirectory();
    }

    private static bool HasBundle(string path)
    {
        return File.Exists(Path.Combine(path, "static.html"))
            || File.Exists(Path.Combine(path, "index.html"));
    }

    private static NodeModulesState GetNodeModulesState(string frontendRoot)
    {
        var nodeModulesPath = Path.Combine(frontendRoot, "node_modules");
        if (!Directory.Exists(nodeModulesPath))
        {
            return NodeModulesState.Missing;
        }

        var toolsPath = Path.Combine(nodeModulesPath, ".bin");
        return Directory.Exists(toolsPath)
            ? NodeModulesState.Ready
            : NodeModulesState.Incomplete;
    }

    private async Task<string> ResolveRepositoryBundleAsync(string frontendRoot, CancellationToken cancellationToken)
    {
        var repositoryDist = Path.Combine(frontendRoot, "dist");
        if (HasBundle(repositoryDist) && !IsBundleStale(frontendRoot, repositoryDist))
        {
            return repositoryDist;
        }

        await BuildBundleAsync(frontendRoot, repositoryDist, cancellationToken);
        if (HasBundle(repositoryDist))
        {
            return repositoryDist;
        }

        throw new CliUsageException($"InSpectra.UI bundle is missing after the build attempt. {FrontendBuildHint}");
    }

    private static bool HasFrontendProject(string frontendRoot)
    {
        return Directory.Exists(frontendRoot)
            && File.Exists(Path.Combine(frontendRoot, "package.json"))
            && File.Exists(Path.Combine(frontendRoot, "package-lock.json"));
    }

    private static bool IsBundleStale(string frontendRoot, string bundleRoot)
    {
        var bundleWriteTime = GetLatestWriteTimeUtc(bundleRoot);
        foreach (var inputPath in EnumerateFrontendInputs(frontendRoot))
        {
            if (File.GetLastWriteTimeUtc(inputPath) > bundleWriteTime)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateFrontendInputs(string frontendRoot)
    {
        foreach (var relativePath in FrontendInputFiles)
        {
            var absolutePath = Path.Combine(frontendRoot, relativePath);
            if (File.Exists(absolutePath))
            {
                yield return absolutePath;
            }
        }

        var sourceRoot = Path.Combine(frontendRoot, "src");
        if (!Directory.Exists(sourceRoot))
        {
            yield break;
        }

        foreach (var sourcePath in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            yield return sourcePath;
        }
    }

    private static DateTime GetLatestWriteTimeUtc(string directoryPath)
    {
        return Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
            .Select(File.GetLastWriteTimeUtc)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();
    }

    private enum NodeModulesState
    {
        Missing,
        Incomplete,
        Ready,
    }
}
