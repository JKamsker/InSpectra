using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Rendering.Contracts;
using InSpectra.Gen.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace InSpectra.Gen.Tests.Rendering;

public class HtmlRenderServiceTests
{
    private static readonly HtmlFeatureFlags DefaultFeatures = new(
        ShowHome: false,
        Composer: true,
        DarkTheme: true,
        LightTheme: true,
        UrlLoading: false,
        NugetBrowser: false,
        PackageUpload: false,
        ColorThemePicker: false);

    [Fact]
    public async Task File_render_writes_bundle_and_injects_inline_bootstrap()
    {
        using var temp = new TempDirectory();
        var bundleRoot = CreateBundle(temp.Path, "packaged");
        var service = CreateHtmlRenderService(new ViewerBundleLocatorOptions
        {
            PackagedRootPath = bundleRoot,
            RepositoryRootPath = temp.Path,
        });

        var outputDirectory = Path.Combine(temp.Path, "html");
        var request = new FileRenderRequest(
            FixturePaths.OpenCliJson,
            FixturePaths.XmlDoc,
            new RenderExecutionOptions(
                RenderLayout.App,
                DryRun: false,
                IncludeHidden: false,
                IncludeMetadata: true,
                Overwrite: false,
                SingleFile: false,
                CompressLevel: 0,
                OutputFile: null,
                OutputDirectory: outputDirectory));

        var result = await service.RenderFromFileAsync(request, DefaultFeatures, CancellationToken.None, title: "JellyfinCli", commandPrefix: "jf");

        var indexPath = Path.Combine(outputDirectory, "index.html");
        var index = await File.ReadAllTextAsync(indexPath);

        Assert.Equal(DocumentFormat.Html, result.Format);
        Assert.Equal(RenderLayout.App, result.Layout);
        Assert.Contains(result.Files, file => file.RelativePath == "index.html");
        Assert.Contains(result.Files, file => file.RelativePath == "assets/app.js");
        Assert.Contains("\"mode\":\"inline\"", index);
        Assert.Contains("\"xmlDoc\":", index);
        Assert.Contains("\"includeMetadata\":true", index);
        Assert.Contains("\"title\":\"JellyfinCli\"", index);
        Assert.Contains("\"commandPrefix\":\"jf\"", index);
        Assert.Contains("jdr", index);
    }

    [Fact]
    public async Task Dry_run_plans_bundle_files_without_writing_output()
    {
        using var temp = new TempDirectory();
        var bundleRoot = CreateBundle(temp.Path, "packaged");
        var service = CreateHtmlRenderService(new ViewerBundleLocatorOptions
        {
            PackagedRootPath = bundleRoot,
            RepositoryRootPath = temp.Path,
        });

        var outputDirectory = Path.Combine(temp.Path, "html");
        var request = new FileRenderRequest(
            FixturePaths.OpenCliJson,
            FixturePaths.XmlDoc,
            new RenderExecutionOptions(
                RenderLayout.App,
                DryRun: true,
                IncludeHidden: false,
                IncludeMetadata: false,
                Overwrite: false,
                SingleFile: false,
                CompressLevel: 0,
                OutputFile: null,
                OutputDirectory: outputDirectory));

        var result = await service.RenderFromFileAsync(request, DefaultFeatures, CancellationToken.None);

        Assert.True(result.IsDryRun);
        Assert.Equal(3, result.Files.Count);
        Assert.DoesNotContain(result.Files, file => file.Content is not null);
        Assert.False(Directory.Exists(outputDirectory));
    }

    [Fact]
    public async Task Dry_run_does_not_build_stale_repo_bundle()
    {
        using var temp = new TempDirectory();
        var repositoryRoot = CreateRepositoryBundle(Path.Combine(temp.Path, "repo"));
        var frontendRoot = CreateFrontendInputs(repositoryRoot);
        var staleSourcePath = Path.Combine(frontendRoot, "src", "viewer.ts");
        Directory.CreateDirectory(Path.GetDirectoryName(staleSourcePath)!);
        File.WriteAllText(staleSourcePath, "export const viewer = true;");

        var staleTime = DateTime.UtcNow.AddMinutes(-5);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "index.html"), staleTime);
        File.SetLastWriteTimeUtc(Path.Combine(frontendRoot, "dist", "static.html"), staleTime);
        File.SetLastWriteTimeUtc(staleSourcePath, DateTime.UtcNow);

        var locator = new TestViewerBundleLocator(
            Options.Create(new ViewerBundleLocatorOptions
            {
                PackagedRootPath = Path.Combine(temp.Path, "missing"),
                RepositoryRootPath = repositoryRoot,
            }));
        var service = new HtmlRenderService(
            RendererFactory.CreateDocumentRenderService(),
            new OpenCliNormalizer(),
            locator,
            new RenderStatsFactory());

        var outputDirectory = Path.Combine(temp.Path, "html");
        var request = new FileRenderRequest(
            FixturePaths.OpenCliJson,
            FixturePaths.XmlDoc,
            new RenderExecutionOptions(
                RenderLayout.App,
                DryRun: true,
                IncludeHidden: false,
                IncludeMetadata: false,
                Overwrite: false,
                SingleFile: false,
                CompressLevel: 1,
                OutputFile: null,
                OutputDirectory: outputDirectory));

        var result = await service.RenderFromFileAsync(request, DefaultFeatures, CancellationToken.None);

        Assert.False(locator.BuildInvoked);
        Assert.True(result.IsDryRun);
        Assert.False(Directory.Exists(outputDirectory));
    }

    [Fact]
    public async Task Single_file_render_writes_only_index_html_with_inlined_assets()
    {
        using var temp = new TempDirectory();
        var bundleRoot = CreateBundle(temp.Path, "packaged");
        var service = CreateHtmlRenderService(new ViewerBundleLocatorOptions
        {
            PackagedRootPath = bundleRoot,
            RepositoryRootPath = temp.Path,
        });

        var outputDirectory = Path.Combine(temp.Path, "html");
        var request = new FileRenderRequest(
            FixturePaths.OpenCliJson,
            FixturePaths.XmlDoc,
            new RenderExecutionOptions(
                RenderLayout.App,
                DryRun: false,
                IncludeHidden: false,
                IncludeMetadata: false,
                Overwrite: false,
                SingleFile: true,
                CompressLevel: 1,
                OutputFile: null,
                OutputDirectory: outputDirectory));

        var result = await service.RenderFromFileAsync(request, DefaultFeatures, CancellationToken.None);

        var indexPath = Path.Combine(outputDirectory, "index.html");
        var index = await File.ReadAllTextAsync(indexPath);

        Assert.Single(result.Files);
        Assert.Equal("index.html", result.Files[0].RelativePath);
        Assert.True(File.Exists(indexPath));
        Assert.False(Directory.Exists(Path.Combine(outputDirectory, "assets")));
        Assert.DoesNotContain("src=\"./assets/app.js\"", index, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"./assets/app.css\"", index, StringComparison.Ordinal);
        Assert.Contains("console.log('bundle');", index);
        Assert.Contains("body { color: black; }", index);
    }

    [Fact]
    public async Task Render_rejects_incomplete_bundle_assets()
    {
        using var temp = new TempDirectory();
        var bundleRoot = CreateBundle(temp.Path, "packaged");
        File.Delete(Path.Combine(bundleRoot, "assets", "app.css"));
        var service = CreateHtmlRenderService(new ViewerBundleLocatorOptions
        {
            PackagedRootPath = bundleRoot,
            RepositoryRootPath = temp.Path,
        });

        var request = new FileRenderRequest(
            FixturePaths.OpenCliJson,
            FixturePaths.XmlDoc,
            new RenderExecutionOptions(
                RenderLayout.App,
                DryRun: false,
                IncludeHidden: false,
                IncludeMetadata: false,
                Overwrite: false,
                SingleFile: false,
                CompressLevel: 0,
                OutputFile: null,
                OutputDirectory: Path.Combine(temp.Path, "html")));

        var exception = await Assert.ThrowsAsync<CliUsageException>(() =>
            service.RenderFromFileAsync(request, DefaultFeatures, CancellationToken.None));

        Assert.Contains("incomplete", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("app.css", string.Join(Environment.NewLine, exception.Details));
    }

    private static HtmlRenderService CreateHtmlRenderService(ViewerBundleLocatorOptions options)
    {
        return new HtmlRenderService(
            RendererFactory.CreateDocumentRenderService(),
            new OpenCliNormalizer(),
            new ViewerBundleLocator(Options.Create(options)),
            new RenderStatsFactory());
    }

    private static string CreateBundle(string rootPath, string folderName)
    {
        var bundleRoot = Path.Combine(rootPath, folderName);
        Directory.CreateDirectory(Path.Combine(bundleRoot, "assets"));
        File.WriteAllText(Path.Combine(bundleRoot, "static.html"),
            """<!doctype html><head><script type="module" src="./assets/app.js"></script><link rel="stylesheet" href="./assets/app.css"></head><body><div id="root"></div><script id="inspectra-bootstrap" type="application/json">__INSPECTRA_BOOTSTRAP__</script></body></html>""");
        File.WriteAllText(Path.Combine(bundleRoot, "index.html"), "<!doctype html><div id=\"root\"></div>");
        File.WriteAllText(Path.Combine(bundleRoot, "assets", "app.js"), "console.log('bundle');");
        File.WriteAllText(Path.Combine(bundleRoot, "assets", "app.css"), "body { color: black; }");
        return bundleRoot;
    }

    private static string CreateRepositoryBundle(string repositoryRoot)
    {
        CreateBundle(Path.Combine(repositoryRoot, "src", "InSpectra.UI"), "dist");
        return repositoryRoot;
    }

    private static string CreateFrontendInputs(string repositoryRoot)
    {
        var frontendRoot = Path.Combine(repositoryRoot, "src", "InSpectra.UI");
        Directory.CreateDirectory(Path.Combine(frontendRoot, "src"));
        File.WriteAllText(Path.Combine(frontendRoot, "package.json"), "{}");
        File.WriteAllText(Path.Combine(frontendRoot, "package-lock.json"), "{}");
        File.WriteAllText(Path.Combine(frontendRoot, "vite.config.ts"), "export default {};");
        File.WriteAllText(Path.Combine(frontendRoot, "tsconfig.json"), "{}");
        return frontendRoot;
    }

    private sealed class TestViewerBundleLocator(
        IOptions<ViewerBundleLocatorOptions> options)
        : ViewerBundleLocator(options)
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
}
