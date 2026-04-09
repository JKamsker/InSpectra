using InSpectra.Gen.Runtime;
using InSpectra.Gen.Services;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests;

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
                ResolvedOutputMode.Human,
                DryRun: false,
                Quiet: false,
                Verbose: false,
                NoColor: false,
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
    public async Task Exec_render_writes_bundle_and_tracks_exec_source()
    {
        using var temp = new TempDirectory();
        var bundleRoot = CreateBundle(temp.Path, "packaged");
        var scriptPath = CreateOpenCliScript(temp.Path);
        var service = CreateHtmlRenderService(new ViewerBundleLocatorOptions
        {
            PackagedRootPath = bundleRoot,
            RepositoryRootPath = temp.Path,
        });

        var outputDirectory = Path.Combine(temp.Path, "html");
        var request = new ExecRenderRequest(
            "pwsh",
            ["-NoProfile", "-File", scriptPath],
            ["cli", "opencli"],
            IncludeXmlDoc: true,
            ["cli", "xmldoc"],
            temp.Path,
            TimeoutSeconds: 30,
            new RenderExecutionOptions(
                RenderLayout.App,
                ResolvedOutputMode.Human,
                DryRun: false,
                Quiet: false,
                Verbose: false,
                NoColor: false,
                IncludeHidden: false,
                IncludeMetadata: false,
                Overwrite: false,
                SingleFile: false,
                CompressLevel: 0,
                OutputFile: null,
                OutputDirectory: outputDirectory));

        var result = await service.RenderFromExecAsync(request, DefaultFeatures, CancellationToken.None);
        var index = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "index.html"));

        Assert.Equal("exec", result.Source.Kind);
        Assert.Contains("\"includeHidden\":false", index);
        Assert.Contains("\"xmlDoc\":", index);
        Assert.Contains("auth", index);
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
                ResolvedOutputMode.Human,
                DryRun: true,
                Quiet: false,
                Verbose: false,
                NoColor: false,
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
        Assert.Contains("3 files planned", result.Summary);
    }

    private static HtmlRenderService CreateHtmlRenderService(ViewerBundleLocatorOptions options)
    {
        return new HtmlRenderService(
            RendererFactory.CreateDocumentRenderService(),
            new OpenCliNormalizer(),
            new ViewerBundleLocator(new ExecutableResolver(), new ProcessRunner(), options),
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

    private static string CreateOpenCliScript(string rootPath)
    {
        var scriptPath = Path.Combine(rootPath, "opencli-fixture.ps1");
        var openCliPath = EscapePowerShellPath(FixturePaths.OpenCliJson);
        var xmlDocPath = EscapePowerShellPath(FixturePaths.XmlDoc);
        var script = $$"""
            param([Parameter(ValueFromRemainingArguments = $true)][string[]] $Args)
            $joined = $Args -join ' '
            if ($joined -like '*cli opencli') {
                Get-Content -Raw '{{openCliPath}}'
                exit 0
            }
            if ($joined -like '*cli xmldoc') {
                Get-Content -Raw '{{xmlDocPath}}'
                exit 0
            }
            Write-Error "Unexpected arguments: $joined"
            exit 1
            """;

        File.WriteAllText(scriptPath, script);
        return scriptPath;
    }

    private static string EscapePowerShellPath(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
