using System.Text.Json.Nodes;
using InSpectra.Gen.Cli;
using InSpectra.Gen.Cli.Output;
using InSpectra.Lib.UseCases.Generate;
using InSpectra.Lib.UseCases.Generate.Requests;
using InSpectra.Lib.Rendering.Contracts;

namespace InSpectra.Gen.Tests.Output;

[Collection(OutputConsoleCollection.Name)]
public class OutputContractTests
{
    [Fact]
    public async Task Json_output_writer_emits_versioned_success_envelope()
    {
        var original = Console.Out;
        await using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            var result = new RenderExecutionResult
            {
                Format = DocumentFormat.Html,
                Layout = RenderLayout.App,
                Source = new RenderSourceInfo("file", "sample.json", null, null),
                Acquisition = new OpenCliAcquisitionMetadata(
                    "help",
                    "tool",
                    "CliFx",
                    [new OpenCliAcquisitionAttempt("native", null, "failed", "native unavailable"), new OpenCliAcquisitionAttempt("help", null, "success")],
                    "sample-opencli.json",
                    "sample-crawl.json"),
                Stats = new RenderStats(1, 2, 3, 1),
                Warnings = [],
                IsDryRun = false,
                Files = [new RenderedFile("index.html", "C:\\temp\\index.html", null)],
            };

            var exitCode = await RenderOutputHandler.ExecuteAsync(
                ResolvedOutputMode.Json,
                quiet: false,
                verbose: false,
                () => Task.FromResult(result));

            var json = JsonNode.Parse(writer.ToString());

            Assert.Equal(0, exitCode);
            Assert.NotNull(json);
            Assert.True(json!["ok"]!.GetValue<bool>());
            Assert.Equal("html", json["data"]!["format"]!.GetValue<string>());
            Assert.Equal("app", json["data"]!["layout"]!.GetValue<string>());
            Assert.Equal("help", json["data"]!["acquisition"]!["selectedMode"]!.GetValue<string>());
            Assert.Equal("sample-crawl.json", json["data"]!["acquisition"]!["artifacts"]!["crawl"]!.GetValue<string>());
            var files = json["data"]!["output"]!["files"]!.AsArray();
            var file = Assert.Single(files);
            Assert.Equal("index.html", file!["relativePath"]!.GetValue<string>());
            Assert.Equal(1, json["meta"]!["schemaVersion"]!.GetValue<int>());
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Fact]
    public async Task Json_output_writer_preserves_hybrid_layout()
    {
        var original = Console.Out;
        await using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            var result = new RenderExecutionResult
            {
                Format = DocumentFormat.Markdown,
                Layout = RenderLayout.Hybrid,
                Source = new RenderSourceInfo("file", "sample.json", null, null),
                Stats = new RenderStats(1, 2, 3, 2),
                Warnings = [],
                IsDryRun = true,
                Files = [new RenderedFile("README.md", "C:\\temp\\README.md", null), new RenderedFile("tree/index.md", "C:\\temp\\tree\\index.md", null)],
            };

            var exitCode = await RenderOutputHandler.ExecuteAsync(
                ResolvedOutputMode.Json,
                quiet: false,
                verbose: false,
                () => Task.FromResult(result));

            var json = JsonNode.Parse(writer.ToString());

            Assert.Equal(0, exitCode);
            Assert.Equal("hybrid", json!["data"]!["layout"]!.GetValue<string>());
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Fact]
    public async Task Json_output_writer_emits_cancellation_envelope()
    {
        var original = Console.Out;
        await using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            var exitCode = await RenderOutputHandler.ExecuteAsync(
                ResolvedOutputMode.Json,
                quiet: false,
                verbose: false,
                () => throw new OperationCanceledException());

            var json = JsonNode.Parse(writer.ToString());

            Assert.Equal(130, exitCode);
            Assert.False(json!["ok"]!.GetValue<bool>());
            Assert.Equal("cancelled", json["error"]!["kind"]!.GetValue<string>());
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Fact]
    public async Task Generate_json_output_writer_emits_acquisition_metadata()
    {
        var original = Console.Out;
        await using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            var result = new GenerateExecutionResult(
                new RenderSourceInfo("dotnet", "src/InSpectra.Gen", "docs/inspectra-gen/xmldoc.xml", "C:\\temp\\inspectra.exe"),
                new OpenCliAcquisitionMetadata(
                    "native",
                    "inspectra",
                    "Spectre.Console.Cli",
                    [new OpenCliAcquisitionAttempt("native", "Spectre.Console.Cli", "success")],
                    "generated-opencli.json",
                    null),
                [],
                "{}",
                "generated-opencli.json");

            var exitCode = await GenerateOutputHandler.ExecuteAsync(
                ResolvedOutputMode.Json,
                verbose: false,
                () => Task.FromResult(result));

            var json = JsonNode.Parse(writer.ToString());

            Assert.Equal(0, exitCode);
            Assert.NotNull(json);
            Assert.True(json!["ok"]!.GetValue<bool>());
            Assert.Equal("dotnet", json["data"]!["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("docs/inspectra-gen/xmldoc.xml", json["data"]!["source"]!["xmlDoc"]!.GetValue<string>());
            Assert.Equal("C:\\temp\\inspectra.exe", json["data"]!["source"]!["executablePath"]!.GetValue<string>());
            Assert.Equal("native", json["data"]!["acquisition"]!["selectedMode"]!.GetValue<string>());
            Assert.Equal("generated-opencli.json", json["data"]!["acquisition"]!["artifacts"]!["openCli"]!.GetValue<string>());
            var attempts = json["data"]!["acquisition"]!["attempts"]!.AsArray();
            var attempt = Assert.Single(attempts);
            Assert.Equal("success", attempt!["outcome"]!.GetValue<string>());
            Assert.Equal(1, json["meta"]!["schemaVersion"]!.GetValue<int>());
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Fact]
    public async Task Human_output_writes_warnings_to_stderr()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        await using var stdout = new StringWriter();
        await using var stderr = new StringWriter();
        Console.SetOut(stdout);
        Console.SetError(stderr);

        try
        {
            var renderResult = new RenderExecutionResult
            {
                Format = DocumentFormat.Markdown,
                Layout = RenderLayout.Single,
                Source = new RenderSourceInfo("file", "sample.json", null, null),
                Stats = new RenderStats(1, 0, 0, 0),
                Warnings = ["warning one"],
                IsDryRun = false,
                StdoutDocument = "content",
                Files = [],
            };

            var generateResult = new GenerateExecutionResult(
                new RenderSourceInfo("exec", "demo", null, "demo"),
                new OpenCliAcquisitionMetadata(
                    "auto",
                    "demo",
                    null,
                    [new OpenCliAcquisitionAttempt("auto", null, "success")],
                    null,
                    null),
                ["warning two"],
                "{}",
                null);

            await RenderOutputHandler.ExecuteAsync(
                ResolvedOutputMode.Human,
                quiet: false,
                verbose: false,
                () => Task.FromResult(renderResult));
            await GenerateOutputHandler.ExecuteAsync(
                ResolvedOutputMode.Human,
                verbose: false,
                () => Task.FromResult(generateResult));

            var errorOutput = stderr.ToString();
            Assert.Contains("Warning: warning one", errorOutput);
            Assert.Contains("Warning: warning two", errorOutput);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

}
