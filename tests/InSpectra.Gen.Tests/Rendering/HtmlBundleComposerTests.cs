using System.IO.Compression;
using System.Text;
using System.Text.Json;
using InSpectra.Gen.Engine.Rendering.Pipeline.Model;
using InSpectra.Gen.Engine.UseCases.Generate.Requests;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.Rendering;

public sealed class HtmlBundleComposerTests
{
    [Fact]
    public void InlineAssets_Inlines_All_Script_Tags_In_Order()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(temp.Path, "assets"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "assets", "chunks"));
        File.WriteAllText(Path.Combine(temp.Path, "assets", "app.js"), "console.log('app');");
        File.WriteAllText(Path.Combine(temp.Path, "assets", "chunks", "extra.js"), "console.log('extra');");

        const string html =
            """<!doctype html><html><body><script src="./assets/app.js"></script><script src="./assets/chunks/extra.js"></script></body></html>""";

        var bundled = HtmlBundleComposer.InlineAssets(html, temp.Path);

        Assert.DoesNotContain("src=\"./assets/app.js\"", bundled);
        Assert.DoesNotContain("src=\"./assets/chunks/extra.js\"", bundled);
        Assert.Contains("console.log('app');", bundled);
        Assert.Contains("console.log('extra');", bundled);
        Assert.True(bundled.IndexOf("console.log('app');", StringComparison.Ordinal) < bundled.IndexOf("console.log('extra');", StringComparison.Ordinal));
    }

    [Fact]
    public void InlineAssets_Preserves_Compressed_Bootstrap_Script()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(temp.Path, "assets"));
        File.WriteAllText(Path.Combine(temp.Path, "assets", "app.js"), "console.log('app');");
        var compressedBootstrap = CreateCompressedPayload("""{"mode":"inline"}""");
        var html =
            $"""<!doctype html><html><body><script id="inspectra-bootstrap" type="application/json">{compressedBootstrap}</script><script src="./assets/app.js"></script></body></html>""";

        var bundled = HtmlBundleComposer.InlineAssets(html, temp.Path);

        Assert.Contains(compressedBootstrap, bundled);
        Assert.Contains("id=\"inspectra-bootstrap\" type=\"application/json\"", bundled, StringComparison.Ordinal);
        Assert.DoesNotContain("window.__inspectraBootstrap", bundled, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSelfExtractingHtml_Packs_All_Referenced_Scripts()
    {
        using var temp = new TempDirectory();
        var bundleRoot = Path.Combine(temp.Path, "bundle");
        var outputDirectory = Path.Combine(temp.Path, "out");
        Directory.CreateDirectory(Path.Combine(bundleRoot, "assets"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "assets"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "assets", "chunks"));
        File.WriteAllText(
            Path.Combine(bundleRoot, "static.html"),
            """<!doctype html><html><head><link rel="stylesheet" href="./assets/app.css"></head><body><script type="module" src="./assets/app.js"></script><script type="module" src="./assets/chunks/extra.js"></script></body></html>""");
        File.WriteAllText(Path.Combine(outputDirectory, "assets", "app.css"), "body { color: black; }");
        File.WriteAllText(Path.Combine(outputDirectory, "assets", "app.js"), "console.log('app');");
        File.WriteAllText(Path.Combine(outputDirectory, "assets", "chunks", "extra.js"), "console.log('extra');");

        var html = HtmlBundleComposer.BuildSelfExtractingHtml(
            CreatePreparedDocument(),
            new RenderExecutionOptions(
                RenderLayout.App,
                DryRun: false,
                IncludeHidden: false,
                IncludeMetadata: false,
                Overwrite: true,
                SingleFile: true,
                CompressLevel: 2,
                OutputFile: null,
                OutputDirectory: outputDirectory),
            new HtmlFeatureFlags(false, true, true, true, false, false, false, false),
            label: null,
            title: null,
            commandPrefix: null,
            themeOptions: null,
            bundleRoot,
            outputDirectory);

        var payload = DecompressPayload(html);

        Assert.Contains("console.log('app');", payload.js);
        Assert.Contains("console.log('extra');", payload.js);
        Assert.Contains("body { color: black; }", payload.css);
    }

    private static (string css, string js, string bootstrap) DecompressPayload(string html)
    {
        var start = html.IndexOf("<script id=\"_z\" type=\"text/plain\">", StringComparison.Ordinal);
        var end = html.IndexOf("</script>", start, StringComparison.Ordinal);
        var base64 = html.Substring(start + "<script id=\"_z\" type=\"text/plain\">".Length, end - start - "<script id=\"_z\" type=\"text/plain\">".Length);
        var bytes = Convert.FromBase64String(base64);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);

        var payload = JsonDocument.Parse(Encoding.UTF8.GetString(output.ToArray())).RootElement;
        return (
            payload.GetProperty("c").GetString() ?? string.Empty,
            payload.GetProperty("j").GetString() ?? string.Empty,
            payload.GetProperty("b").GetString() ?? string.Empty);
    }

    private static string CreateCompressedPayload(string payload)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            var bytes = Encoding.UTF8.GetBytes(payload);
            gzip.Write(bytes, 0, bytes.Length);
        }

        return Convert.ToBase64String(output.ToArray());
    }

    private static AcquiredRenderDocument CreatePreparedDocument()
        => new()
        {
            RawDocument = new OpenCliDocument
            {
                OpenCliVersion = "0.1-draft",
                Info = new OpenCliInfo
                {
                    Title = "demo",
                    Version = "1.0.0",
                },
            },
            RenderDocument = new OpenCliDocument
            {
                OpenCliVersion = "0.1-draft",
                Info = new OpenCliInfo
                {
                    Title = "demo",
                    Version = "1.0.0",
                },
            },
            Source = new RenderSourceInfo("file", "demo.json", null, null),
            Warnings = [],
        };
}
