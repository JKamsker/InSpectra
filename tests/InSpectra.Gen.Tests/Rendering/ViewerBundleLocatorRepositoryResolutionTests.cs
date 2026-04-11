using InSpectra.Gen.Core;
using InSpectra.Gen.Tests.TestSupport;
using Microsoft.Extensions.Options;

using static InSpectra.Gen.Tests.Rendering.ViewerBundleLocatorTestSupport;

namespace InSpectra.Gen.Tests.Rendering;

public sealed class ViewerBundleLocatorRepositoryResolutionTests
{
    [Fact]
    public async Task Repo_bundle_is_used_when_packaged_bundle_is_missing()
    {
        using var temp = new TempDirectory();
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));
        var frontendRoot = CreateFrontendInputs(repositoryRoot);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "index.html"), DateTime.UtcNow.AddMinutes(1));
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "static.html"), DateTime.UtcNow.AddMinutes(1));

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
        var sourcePath = CreateStaleSource(frontendRoot);

        var staleTime = DateTime.UtcNow.AddMinutes(-5);
        var freshTime = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "index.html"), staleTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "static.html"), staleTime);
        File.SetLastWriteTimeUtc(sourcePath, freshTime);

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
    public async Task Stale_packaged_bundle_remains_available_when_repo_build_fails()
    {
        using var temp = new TempDirectory();
        var packagedRoot = CreateBundle(Path.Combine(temp.Path, "packaged"));
        var repositoryRoot = Path.Combine(temp.Path, "repo");
        var frontendRoot = CreateFrontendInputs(repositoryRoot);
        var sourcePath = CreateStaleSource(frontendRoot);

        var staleTime = DateTime.UtcNow.AddMinutes(-5);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "index.html"), staleTime);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "static.html"), staleTime);
        File.SetLastWriteTimeUtc(sourcePath, DateTime.UtcNow);

        var locator = new FailingViewerBundleLocator(
            new ExecutableResolver(),
            new ProcessRunner(),
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = packagedRoot,
                RepositoryRootPath = repositoryRoot,
            }));

        var resolved = await locator.ResolveAsync(CancellationToken.None);

        Assert.True(locator.BuildInvoked);
        Assert.Equal(packagedRoot, resolved);
    }

    [Fact]
    public async Task Stale_packaged_bundle_remains_available_when_stale_repo_rebuild_fails()
    {
        using var temp = new TempDirectory();
        var packagedRoot = CreateBundle(Path.Combine(temp.Path, "packaged"));
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));
        var frontendRoot = CreateFrontendInputs(repositoryRoot);
        var sourcePath = CreateStaleSource(frontendRoot);

        var staleTime = DateTime.UtcNow.AddMinutes(-5);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "index.html"), staleTime);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "static.html"), staleTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "index.html"), staleTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "static.html"), staleTime);
        File.SetLastWriteTimeUtc(sourcePath, DateTime.UtcNow);

        var locator = new FailingViewerBundleLocator(
            new ExecutableResolver(),
            new ProcessRunner(),
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = packagedRoot,
                RepositoryRootPath = repositoryRoot,
            }));

        var resolved = await locator.ResolveAsync(CancellationToken.None);

        Assert.True(locator.BuildInvoked);
        Assert.Equal(packagedRoot, resolved);
    }

    [Fact]
    public async Task Newer_stale_repo_bundle_is_used_when_rebuild_fails()
    {
        using var temp = new TempDirectory();
        var packagedRoot = CreateBundle(Path.Combine(temp.Path, "packaged"));
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));
        var frontendRoot = CreateFrontendInputs(repositoryRoot);
        var sourcePath = CreateStaleSource(frontendRoot);

        var packagedTime = DateTime.UtcNow.AddMinutes(-5);
        var repositoryTime = DateTime.UtcNow.AddMinutes(-3);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "index.html"), packagedTime);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "static.html"), packagedTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "index.html"), repositoryTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "static.html"), repositoryTime);
        File.SetLastWriteTimeUtc(sourcePath, DateTime.UtcNow);

        var locator = new FailingViewerBundleLocator(
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
}
