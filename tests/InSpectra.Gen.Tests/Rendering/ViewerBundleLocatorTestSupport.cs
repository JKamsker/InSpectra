using InSpectra.Gen.Core;
using InSpectra.Gen.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace InSpectra.Gen.Tests.Rendering;

internal static class ViewerBundleLocatorTestSupport
{
    public static string CreateBundle(string bundleRoot)
    {
        Directory.CreateDirectory(bundleRoot);
        File.WriteAllText(Path.Combine(bundleRoot, "index.html"), "<!doctype html>");
        File.WriteAllText(Path.Combine(bundleRoot, "static.html"), "<!doctype html>");
        return bundleRoot;
    }

    public static string CreateRepositoryBundle(string repositoryRoot)
    {
        var bundleRoot = Path.Combine(repositoryRoot, "src", "InSpectra.UI", "dist");
        Directory.CreateDirectory(bundleRoot);
        File.WriteAllText(Path.Combine(bundleRoot, "index.html"), "<!doctype html>");
        File.WriteAllText(Path.Combine(bundleRoot, "static.html"), "<!doctype html>");
        return repositoryRoot;
    }

    public static string CreateFrontendInputs(string repositoryRoot)
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

    public static string CreateStaleSource(string frontendRoot)
    {
        var sourcePath = Path.Combine(frontendRoot, "src", "viewer.ts");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        File.WriteAllText(sourcePath, "export const viewer = true;");
        return sourcePath;
    }
}

internal sealed class TestViewerBundleLocator(
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
        File.WriteAllText(Path.Combine(repositoryDist, "static.html"), "<!doctype html>");
        return Task.CompletedTask;
    }
}

internal sealed class FailingViewerBundleLocator(
    ExecutableResolver executableResolver,
    IProcessRunner processRunner,
    IOptions<ViewerBundleLocatorOptions> options)
    : ViewerBundleLocator(executableResolver, processRunner, options)
{
    public bool BuildInvoked { get; private set; }

    protected override Task BuildBundleAsync(string frontendRoot, string repositoryDist, CancellationToken cancellationToken)
    {
        BuildInvoked = true;
        throw new CliUsageException("simulated build failure");
    }
}
