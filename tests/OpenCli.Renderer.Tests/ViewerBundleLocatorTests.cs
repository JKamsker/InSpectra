using OpenCli.Renderer.Services;
using OpenCli.Renderer.Runtime;
using OpenCli.Renderer.Tests.TestSupport;

namespace OpenCli.Renderer.Tests;

public class ViewerBundleLocatorTests
{
    [Fact]
    public async Task Packaged_bundle_is_preferred_over_repo_bundle()
    {
        using var temp = new TempDirectory();
        var packagedRoot = CreateBundle(Path.Combine(temp.Path, "packaged"));
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));

        var locator = new ViewerBundleLocator(
            new ExecutableResolver(),
            new ProcessRunner(),
            new ViewerBundleLocatorOptions
            {
                PackagedRootPath = packagedRoot,
                RepositoryRootPath = repositoryRoot,
            });

        var resolved = await locator.ResolveAsync(CancellationToken.None);

        Assert.Equal(packagedRoot, resolved);
    }

    [Fact]
    public async Task Repo_bundle_is_used_when_packaged_bundle_is_missing()
    {
        using var temp = new TempDirectory();
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));

        var locator = new ViewerBundleLocator(
            new ExecutableResolver(),
            new ProcessRunner(),
            new ViewerBundleLocatorOptions
            {
                PackagedRootPath = Path.Combine(temp.Path, "missing"),
                RepositoryRootPath = repositoryRoot,
            });

        var resolved = await locator.ResolveAsync(CancellationToken.None);

        Assert.Equal(Path.Combine(repositoryRoot, "src", "InSpectreUI", "dist"), resolved);
    }

    [Fact]
    public async Task Missing_repo_bundle_can_be_built_on_demand()
    {
        using var temp = new TempDirectory();
        var repositoryRoot = Path.Combine(temp.Path, "repo");
        var frontendRoot = Path.Combine(repositoryRoot, "src", "InSpectreUI");
        Directory.CreateDirectory(frontendRoot);
        File.WriteAllText(Path.Combine(frontendRoot, "package.json"), "{}");
        File.WriteAllText(Path.Combine(frontendRoot, "package-lock.json"), "{}");

        var locator = new TestViewerBundleLocator(
            new ExecutableResolver(),
            new ProcessRunner(),
            new ViewerBundleLocatorOptions
            {
                PackagedRootPath = Path.Combine(temp.Path, "missing"),
                RepositoryRootPath = repositoryRoot,
            });

        var resolved = await locator.ResolveAsync(CancellationToken.None);

        Assert.True(locator.BuildInvoked);
        Assert.Equal(Path.Combine(frontendRoot, "dist"), resolved);
        Assert.True(File.Exists(Path.Combine(resolved, "index.html")));
    }

    [Fact]
    public async Task Missing_repo_sources_fail_with_build_hint()
    {
        using var temp = new TempDirectory();
        var repositoryRoot = Path.Combine(temp.Path, "repo");
        Directory.CreateDirectory(repositoryRoot);

        var locator = new ViewerBundleLocator(
            new ExecutableResolver(),
            new ProcessRunner(),
            new ViewerBundleLocatorOptions
            {
                PackagedRootPath = Path.Combine(temp.Path, "missing"),
                RepositoryRootPath = repositoryRoot,
            });

        var exception = await Assert.ThrowsAsync<CliUsageException>(() => locator.ResolveAsync(CancellationToken.None));

        Assert.Contains("InSpectreUI sources were not found", exception.Message);
        Assert.Contains("npm ci", exception.Message);
        Assert.Contains("npm run build", exception.Message);
    }

    private static string CreateBundle(string bundleRoot)
    {
        Directory.CreateDirectory(bundleRoot);
        File.WriteAllText(Path.Combine(bundleRoot, "index.html"), "<!doctype html>");
        return bundleRoot;
    }

    private static string CreateRepositoryBundle(string repositoryRoot)
    {
        var bundleRoot = Path.Combine(repositoryRoot, "src", "InSpectreUI", "dist");
        Directory.CreateDirectory(bundleRoot);
        File.WriteAllText(Path.Combine(bundleRoot, "index.html"), "<!doctype html>");
        return repositoryRoot;
    }

    private sealed class TestViewerBundleLocator(
        ExecutableResolver executableResolver,
        ProcessRunner processRunner,
        ViewerBundleLocatorOptions options)
        : ViewerBundleLocator(executableResolver, processRunner, options)
    {
        public bool BuildInvoked { get; private set; }

        protected override Task BuildBundleAsync(string frontendRoot, string repositoryDist, CancellationToken cancellationToken)
        {
            BuildInvoked = true;
            Directory.CreateDirectory(repositoryDist);
            File.WriteAllText(Path.Combine(repositoryDist, "index.html"), "<!doctype html>");
            return Task.CompletedTask;
        }
    }
}
