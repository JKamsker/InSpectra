using InSpectra.Gen.Tests.TestSupport;
using Microsoft.Extensions.Options;

using static InSpectra.Gen.Tests.Rendering.ViewerBundleLocatorTestSupport;

namespace InSpectra.Gen.Tests.Rendering;

public sealed class ViewerBundleLocatorMixedStateTests
{
    [Fact]
    public async Task Packaged_bundle_is_preferred_over_repo_bundle()
    {
        using var temp = new TempDirectory();
        var packagedRoot = CreateBundle(Path.Combine(temp.Path, "packaged"));
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));
        _ = CreateFrontendInputs(repositoryRoot);
        var freshTime = DateTime.UtcNow.AddMinutes(1);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "index.html"), freshTime);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "static.html"), freshTime);

        var locator = new TestViewerBundleLocator(
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = packagedRoot,
                RepositoryRootPath = repositoryRoot,
            }));

        var resolved = await locator.ResolveAsync(CancellationToken.None);

        Assert.False(locator.BuildInvoked);
        Assert.Equal(packagedRoot, resolved);
    }

    [Fact]
    public async Task Stale_packaged_bundle_falls_back_to_repo_bundle()
    {
        using var temp = new TempDirectory();
        var packagedRoot = CreateBundle(Path.Combine(temp.Path, "packaged"));
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));
        var frontendRoot = CreateFrontendInputs(repositoryRoot);
        var sourcePath = CreateStaleSource(frontendRoot);

        var staleTime = DateTime.UtcNow.AddMinutes(-5);
        var freshTime = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "index.html"), staleTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "static.html"), staleTime);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "index.html"), staleTime);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "static.html"), staleTime);
        File.SetLastWriteTimeUtc(sourcePath, freshTime);

        var locator = new TestViewerBundleLocator(
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
    public async Task Stale_packaged_bundle_with_missing_repo_dist_is_rebuilt_when_build_is_allowed()
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

        var locator = new TestViewerBundleLocator(
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
    public async Task Packaged_bundle_is_used_when_repo_metadata_exists_but_dist_is_missing_and_build_is_disallowed()
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

        var locator = new TestViewerBundleLocator(
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = packagedRoot,
                RepositoryRootPath = repositoryRoot,
            }));

        var resolved = await locator.ResolveAsync(CancellationToken.None, allowBuild: false);

        Assert.False(locator.BuildInvoked);
        Assert.Equal(packagedRoot, resolved);
    }

    [Fact]
    public async Task Fresh_repo_bundle_is_used_when_packaged_bundle_is_stale()
    {
        using var temp = new TempDirectory();
        var packagedRoot = CreateBundle(Path.Combine(temp.Path, "packaged"));
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));
        var frontendRoot = CreateFrontendInputs(repositoryRoot);
        var sourcePath = CreateStaleSource(frontendRoot);

        var packagedTime = DateTime.UtcNow.AddMinutes(-5);
        var sourceTime = DateTime.UtcNow.AddMinutes(-1);
        var repoTime = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "index.html"), packagedTime);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "static.html"), packagedTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "index.html"), repoTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "static.html"), repoTime);
        File.SetLastWriteTimeUtc(sourcePath, sourceTime);

        var locator = new TestViewerBundleLocator(
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = packagedRoot,
                RepositoryRootPath = repositoryRoot,
            }));

        var resolved = await locator.ResolveAsync(CancellationToken.None);

        Assert.False(locator.BuildInvoked);
        Assert.Equal(Path.Combine(frontendRoot, "dist"), resolved);
    }

    [Fact]
    public async Task Fresh_repo_bundle_is_used_when_packaged_bundle_is_stale_and_build_is_disallowed()
    {
        using var temp = new TempDirectory();
        var packagedRoot = CreateBundle(Path.Combine(temp.Path, "packaged"));
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));
        var frontendRoot = CreateFrontendInputs(repositoryRoot);
        var sourcePath = CreateStaleSource(frontendRoot);

        var packagedTime = DateTime.UtcNow.AddMinutes(-5);
        var sourceTime = DateTime.UtcNow.AddMinutes(-1);
        var repoTime = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "index.html"), packagedTime);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "static.html"), packagedTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "index.html"), repoTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "static.html"), repoTime);
        File.SetLastWriteTimeUtc(sourcePath, sourceTime);

        var locator = new TestViewerBundleLocator(
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = packagedRoot,
                RepositoryRootPath = repositoryRoot,
            }));

        var resolved = await locator.ResolveAsync(CancellationToken.None, allowBuild: false);

        Assert.False(locator.BuildInvoked);
        Assert.Equal(Path.Combine(frontendRoot, "dist"), resolved);
    }

    [Fact]
    public async Task Stale_packaged_bundle_is_used_when_repo_bundle_is_older_and_build_is_disallowed()
    {
        using var temp = new TempDirectory();
        var packagedRoot = CreateBundle(Path.Combine(temp.Path, "packaged"));
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));
        var frontendRoot = CreateFrontendInputs(repositoryRoot);
        var sourcePath = CreateStaleSource(frontendRoot);

        var repositoryTime = DateTime.UtcNow.AddMinutes(-5);
        var packagedTime = DateTime.UtcNow.AddMinutes(-3);
        var freshTime = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "index.html"), repositoryTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "static.html"), repositoryTime);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "index.html"), packagedTime);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "static.html"), packagedTime);
        File.SetLastWriteTimeUtc(sourcePath, freshTime);

        var locator = new TestViewerBundleLocator(
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = packagedRoot,
                RepositoryRootPath = repositoryRoot,
            }));

        var resolved = await locator.ResolveAsync(CancellationToken.None, allowBuild: false);

        Assert.False(locator.BuildInvoked);
        Assert.Equal(packagedRoot, resolved);
    }

    [Fact]
    public async Task Newer_repo_bundle_is_used_when_both_bundles_are_stale_and_build_is_disallowed()
    {
        using var temp = new TempDirectory();
        var packagedRoot = CreateBundle(Path.Combine(temp.Path, "packaged"));
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));
        var frontendRoot = CreateFrontendInputs(repositoryRoot);
        var sourcePath = CreateStaleSource(frontendRoot);

        var packagedTime = DateTime.UtcNow.AddMinutes(-5);
        var repositoryTime = DateTime.UtcNow.AddMinutes(-3);
        var freshTime = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "index.html"), packagedTime);
        File.SetLastWriteTimeUtc(Path.Combine(packagedRoot, "static.html"), packagedTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "index.html"), repositoryTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "static.html"), repositoryTime);
        File.SetLastWriteTimeUtc(sourcePath, freshTime);

        var locator = new TestViewerBundleLocator(
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = packagedRoot,
                RepositoryRootPath = repositoryRoot,
            }));

        var resolved = await locator.ResolveAsync(CancellationToken.None, allowBuild: false);

        Assert.False(locator.BuildInvoked);
        Assert.Equal(Path.Combine(frontendRoot, "dist"), resolved);
    }
}
