using OpenCli.Renderer.Runtime;

namespace OpenCli.Renderer.Services;

public sealed class ViewerBundleLocatorOptions
{
    public string? PackagedRootPath { get; init; }

    public string? RepositoryRootPath { get; init; }

    public string? NpmExecutablePath { get; init; }

    public int NpmTimeoutSeconds { get; init; } = 300;
}

public class ViewerBundleLocator(
    ExecutableResolver executableResolver,
    ProcessRunner processRunner,
    ViewerBundleLocatorOptions options)
{
    private const string FrontendBuildHint = "Run `npm ci` and `npm run build` in `src/InSpectreUI` to build the viewer bundle.";

    public async Task<string> ResolveAsync(CancellationToken cancellationToken)
    {
        var packagedPath = options.PackagedRootPath ?? Path.Combine(AppContext.BaseDirectory, "InSpectreUI", "dist");
        if (HasBundle(packagedPath))
        {
            return packagedPath;
        }

        var repositoryRoot = options.RepositoryRootPath ?? FindRepositoryRoot();
        if (repositoryRoot is null)
        {
            throw new CliUsageException($"InSpectreUI bundle could not be located beside the tool. {FrontendBuildHint}");
        }

        var frontendRoot = Path.Combine(repositoryRoot, "src", "InSpectreUI");
        var repositoryDist = Path.Combine(frontendRoot, "dist");
        if (HasBundle(repositoryDist))
        {
            return repositoryDist;
        }

        await BuildBundleAsync(frontendRoot, repositoryDist, cancellationToken);
        if (HasBundle(repositoryDist))
        {
            return repositoryDist;
        }

        throw new CliUsageException($"InSpectreUI bundle is missing after the build attempt. {FrontendBuildHint}");
    }

    protected virtual async Task BuildBundleAsync(string frontendRoot, string repositoryDist, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(frontendRoot))
        {
            throw new CliUsageException($"InSpectreUI sources were not found at `{frontendRoot}`. {FrontendBuildHint}");
        }

        var packageJsonPath = Path.Combine(frontendRoot, "package.json");
        var packageLockPath = Path.Combine(frontendRoot, "package-lock.json");
        if (!File.Exists(packageJsonPath) || !File.Exists(packageLockPath))
        {
            throw new CliUsageException($"InSpectreUI package metadata is missing in `{frontendRoot}`. {FrontendBuildHint}");
        }

        string npmExecutable;
        try
        {
            npmExecutable = options.NpmExecutablePath ?? executableResolver.Resolve("npm", frontendRoot);
        }
        catch (CliException)
        {
            throw new CliUsageException($"InSpectreUI dist was not found and `npm` is not available on PATH. {FrontendBuildHint}");
        }

        try
        {
            await processRunner.RunAsync(npmExecutable, frontendRoot, ["ci"], options.NpmTimeoutSeconds, cancellationToken);
            await processRunner.RunAsync(npmExecutable, frontendRoot, ["run", "build"], options.NpmTimeoutSeconds, cancellationToken);
        }
        catch (CliException exception)
        {
            throw new CliUsageException(
                $"Failed to build InSpectreUI in `{frontendRoot}`.",
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
                if (File.Exists(Path.Combine(current.FullName, "OpenCli.Renderer.sln")))
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
        return File.Exists(Path.Combine(path, "index.html"));
    }
}
