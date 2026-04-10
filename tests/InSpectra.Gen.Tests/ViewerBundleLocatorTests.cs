using InSpectra.Gen.Services;
using InSpectra.Gen.Runtime;
using InSpectra.Gen.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace InSpectra.Gen.Tests;

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
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = packagedRoot,
                RepositoryRootPath = repositoryRoot,
            }));

        var resolved = await locator.ResolveAsync(CancellationToken.None);

        Assert.Equal(packagedRoot, resolved);
    }

    [Fact]
    public async Task Stale_packaged_bundle_falls_back_to_repo_bundle()
    {
        using var temp = new TempDirectory();
        var packagedRoot = CreateBundle(Path.Combine(temp.Path, "packaged"));
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));
        var frontendRoot = CreateFrontendInputs(repositoryRoot);
        var staleSourcePath = Path.Combine(frontendRoot, "src", "viewer.ts");
        Directory.CreateDirectory(Path.GetDirectoryName(staleSourcePath)!);
        File.WriteAllText(staleSourcePath, "export const viewer = true;");

        var staleTime = DateTime.UtcNow.AddMinutes(-5);
        var freshTime = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "index.html"), staleTime);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "index.html"), staleTime);
        File.SetLastWriteTimeUtc(staleSourcePath, freshTime);

        var locator = new TestViewerBundleLocator(
            new ExecutableResolver(),
            new ProcessRunner(),
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = packagedRoot,
                RepositoryRootPath = repositoryRoot,
            }));

        var resolved = await locator.ResolveAsync(CancellationToken.None);

        Assert.True(locator.BuildInvoked);
        Assert.Equal(Path.Combine(frontendRoot, "dist"), resolved);
    }

    [Fact]
    public async Task Repo_bundle_is_used_when_packaged_bundle_is_missing()
    {
        using var temp = new TempDirectory();
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));
        var frontendRoot = CreateFrontendInputs(repositoryRoot);
        File.SetLastWriteTimeUtc(
            Path.Combine(frontendRoot, "dist", "index.html"),
            DateTime.UtcNow.AddMinutes(1));

        var locator = new ViewerBundleLocator(
            new ExecutableResolver(),
            new ProcessRunner(),
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = Path.Combine(temp.Path, "missing"),
                RepositoryRootPath = repositoryRoot,
            }));

        var resolved = await locator.ResolveAsync(CancellationToken.None);

        Assert.Equal(Path.Combine(repositoryRoot, "src", "InSpectra.UI", "dist"), resolved);
    }

    [Fact]
    public async Task Stale_repo_bundle_is_rebuilt_before_use()
    {
        using var temp = new TempDirectory();
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));
        var frontendRoot = CreateFrontendInputs(repositoryRoot);
        var staleSourcePath = Path.Combine(frontendRoot, "src", "viewer.ts");
        Directory.CreateDirectory(Path.GetDirectoryName(staleSourcePath)!);
        File.WriteAllText(staleSourcePath, "export const viewer = true;");

        var staleTime = DateTime.UtcNow.AddMinutes(-5);
        var freshTime = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "index.html"), staleTime);
        File.SetLastWriteTimeUtc(staleSourcePath, freshTime);

        var locator = new TestViewerBundleLocator(
            new ExecutableResolver(),
            new ProcessRunner(),
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = Path.Combine(temp.Path, "missing"),
                RepositoryRootPath = repositoryRoot,
            }));

        var resolved = await locator.ResolveAsync(CancellationToken.None);

        Assert.True(locator.BuildInvoked);
        Assert.Equal(Path.Combine(frontendRoot, "dist"), resolved);
    }

    [Fact]
    public async Task Missing_repo_bundle_can_be_built_on_demand()
    {
        using var temp = new TempDirectory();
        var repositoryRoot = Path.Combine(temp.Path, "repo");
        var frontendRoot = Path.Combine(repositoryRoot, "src", "InSpectra.UI");
        Directory.CreateDirectory(frontendRoot);
        File.WriteAllText(Path.Combine(frontendRoot, "package.json"), "{}");
        File.WriteAllText(Path.Combine(frontendRoot, "package-lock.json"), "{}");

        var locator = new TestViewerBundleLocator(
            new ExecutableResolver(),
            new ProcessRunner(),
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = Path.Combine(temp.Path, "missing"),
                RepositoryRootPath = repositoryRoot,
            }));

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
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = Path.Combine(temp.Path, "missing"),
                RepositoryRootPath = repositoryRoot,
            }));

        var exception = await Assert.ThrowsAsync<CliUsageException>(() => locator.ResolveAsync(CancellationToken.None));

        Assert.Contains("InSpectra.UI bundle could not be located", exception.Message);
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
        var bundleRoot = Path.Combine(repositoryRoot, "src", "InSpectra.UI", "dist");
        Directory.CreateDirectory(bundleRoot);
        File.WriteAllText(Path.Combine(bundleRoot, "index.html"), "<!doctype html>");
        return repositoryRoot;
    }

    private static string CreateFrontendInputs(string repositoryRoot)
    {
        var frontendRoot = Path.Combine(repositoryRoot, "src", "InSpectra.UI");
        Directory.CreateDirectory(Path.Combine(frontendRoot, "src"));
        File.WriteAllText(Path.Combine(frontendRoot, "package.json"), "{}");
        File.WriteAllText(Path.Combine(frontendRoot, "package-lock.json"), "{}");
        File.WriteAllText(Path.Combine(frontendRoot, "static.html"), "<!doctype html>");
        File.WriteAllText(Path.Combine(frontendRoot, "vite.config.ts"), "export default {};");
        File.WriteAllText(Path.Combine(frontendRoot, "tsconfig.json"), "{}");
        return frontendRoot;
    }

    private sealed class TestViewerBundleLocator(
        ExecutableResolver executableResolver,
        IProcessRunner processRunner,
        IOptions<ViewerBundleLocatorOptions> options)
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
