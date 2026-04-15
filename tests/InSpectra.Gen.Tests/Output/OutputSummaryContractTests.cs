using InSpectra.Gen.Cli;
using InSpectra.Gen.Cli.Output;
using InSpectra.Lib.Rendering.Contracts;

namespace InSpectra.Gen.Tests.Output;

[Collection(OutputConsoleCollection.Name)]
public class OutputSummaryContractTests
{
    [Fact]
    public async Task Human_output_writes_render_summary_when_not_quiet()
    {
        var originalOut = Console.Out;
        await using var stdout = new StringWriter();
        Console.SetOut(stdout);
        var outputDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "inspectra-output-summary", Guid.NewGuid().ToString("N")));
        var outputFile = Path.Combine(outputDirectory, "index.html");

        try
        {
            var result = new RenderExecutionResult
            {
                Format = DocumentFormat.Html,
                Layout = RenderLayout.App,
                Source = new RenderSourceInfo("file", "sample.json", null, null),
                Stats = new RenderStats(1, 0, 0, 1),
                Warnings = [],
                IsDryRun = false,
                Files = [new RenderedFile("index.html", outputFile, null)],
            };

            await RenderOutputHandler.ExecuteAsync(
                ResolvedOutputMode.Human,
                quiet: false,
                verbose: false,
                () => Task.FromResult(result));

            Assert.Contains($"Wrote HTML app bundle (1 files) to `{outputDirectory}`.", stdout.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Quiet_hides_render_summary_for_human_output()
    {
        var originalOut = Console.Out;
        await using var stdout = new StringWriter();
        Console.SetOut(stdout);
        var outputDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "inspectra-output-summary", Guid.NewGuid().ToString("N")));

        try
        {
            var result = new RenderExecutionResult
            {
                Format = DocumentFormat.Markdown,
                Layout = RenderLayout.Tree,
                Source = new RenderSourceInfo("file", "sample.json", null, null),
                Stats = new RenderStats(1, 0, 0, 2),
                Warnings = [],
                IsDryRun = true,
                Files =
                [
                    new RenderedFile("README.md", Path.Combine(outputDirectory, "README.md"), null),
                    new RenderedFile(Path.Combine("nested", "index.md"), Path.Combine(outputDirectory, "nested", "index.md"), null),
                ],
            };

            await RenderOutputHandler.ExecuteAsync(
                ResolvedOutputMode.Human,
                quiet: true,
                verbose: false,
                () => Task.FromResult(result));

            Assert.Equal(string.Empty, stdout.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Human_output_keeps_root_directory_summary()
    {
        var originalOut = Console.Out;
        await using var stdout = new StringWriter();
        Console.SetOut(stdout);
        var rootDirectory = Path.GetPathRoot(Path.GetFullPath(Path.GetTempPath()))
            ?? throw new InvalidOperationException("Expected a filesystem root.");
        var outputFile = Path.Combine(rootDirectory, "inspectra-root-index.html");

        try
        {
            var result = new RenderExecutionResult
            {
                Format = DocumentFormat.Html,
                Layout = RenderLayout.App,
                Source = new RenderSourceInfo("file", "sample.json", null, null),
                Stats = new RenderStats(1, 0, 0, 1),
                Warnings = [],
                IsDryRun = false,
                Files = [new RenderedFile("index.html", outputFile, null)],
            };

            await RenderOutputHandler.ExecuteAsync(
                ResolvedOutputMode.Human,
                quiet: false,
                verbose: false,
                () => Task.FromResult(result));

            Assert.Contains($"Wrote HTML app bundle (1 files) to `{rootDirectory}`.", stdout.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
