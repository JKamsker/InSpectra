using InSpectra.Lib.Rendering.Pipeline.Model;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.Rendering;

public class MarkdownDocumentationCoverageTests
{
    private readonly OpenCliDocumentLoader _loader = new(new OpenCliSchemaProvider());
    private readonly OpenCliXmlEnricher _enricher = new();
    private readonly OpenCliNormalizer _normalizer = new();
    private readonly MarkdownRenderer _renderer = RendererFactory.CreateMarkdownRenderer();
    private readonly CommandPathResolver _pathResolver = new();

    [Fact]
    public async Task Single_markdown_covers_enriched_command_surface()
    {
        var document = await LoadEnrichedDocumentAsync();
        var normalized = _normalizer.Normalize(document, includeHidden: false);

        var markdown = _renderer.RenderSingle(normalized, includeMetadata: true);

        AssertContainsRootSurface(markdown, normalized);
        foreach (var command in normalized.Commands)
        {
            AssertCommandSurface(markdown, command, currentPagePath: null);
        }
    }

    [Fact]
    public async Task Tree_markdown_covers_enriched_command_surface()
    {
        var document = await LoadEnrichedDocumentAsync();
        var normalized = _normalizer.Normalize(document, includeHidden: false);

        var files = _renderer.RenderTree(normalized, includeMetadata: true)
            .ToDictionary(file => file.RelativePath, file => file.Content, StringComparer.Ordinal);

        Assert.True(files.ContainsKey("index.md"));
        AssertContainsRootSurface(files["index.md"], normalized);

        foreach (var command in normalized.Commands)
        {
            AssertTreeCommandSurface(files, command);
        }
    }

    private async Task<OpenCliDocument> LoadEnrichedDocumentAsync()
    {
        var document = await _loader.LoadFromFileAsync(FixturePaths.OpenCliJson, CancellationToken.None);
        await _enricher.EnrichFromFileAsync(document, FixturePaths.XmlDoc, CancellationToken.None);
        return document;
    }

    private void AssertTreeCommandSurface(IReadOnlyDictionary<string, string> files, NormalizedCommand command)
    {
        var relativePath = _pathResolver.GetCommandRelativePath(command, "md");
        Assert.True(files.ContainsKey(relativePath), $"Missing tree page `{relativePath}` for `{command.Path}`.");

        var markdown = files[relativePath];
        AssertCommandSurface(markdown, command, currentPagePath: relativePath);

        foreach (var child in command.Commands)
        {
            AssertTreeCommandSurface(files, child);
        }
    }

    private void AssertContainsRootSurface(string markdown, NormalizedCliDocument document)
    {
        Assert.Contains(document.Source.Info.Title, markdown);
        Assert.Contains(document.Source.Info.Version, markdown);
        AssertContainsIfPresent(markdown, document.Source.Info.Summary);
        AssertContainsIfPresent(markdown, document.Source.Info.Description);

        foreach (var argument in document.RootArguments)
        {
            AssertArgumentSurface(markdown, argument, "root");
        }

        foreach (var option in document.RootOptions)
        {
            AssertOptionSurface(markdown, new ResolvedOption
            {
                Option = option,
                IsInherited = false,
            }, "root");
        }

        foreach (var example in document.Source.Examples)
        {
            Assert.Contains(example, markdown);
        }

        foreach (var exitCode in document.Source.ExitCodes)
        {
            Assert.Contains(exitCode.Code.ToString(), markdown);
            AssertContainsIfPresent(markdown, exitCode.Description);
        }

        foreach (var metadata in document.Source.Metadata)
        {
            Assert.Contains(metadata.Name, markdown);
        }
    }

    private void AssertCommandSurface(string markdown, NormalizedCommand command, string? currentPagePath)
    {
        Assert.Contains(command.Path, markdown);
        AssertContainsIfPresent(markdown, command.Command.Description);

        foreach (var alias in command.Command.Aliases)
        {
            Assert.Contains(alias, markdown);
        }

        foreach (var child in command.Commands)
        {
            Assert.Contains(child.Command.Name, markdown);
            if (currentPagePath is not null)
            {
                var childPath = _pathResolver.GetCommandRelativePath(child, "md");
                var link = _pathResolver.CreateRelativeLink(currentPagePath, childPath);
                Assert.Contains(link, markdown);
            }
        }

        foreach (var argument in command.Arguments)
        {
            AssertArgumentSurface(markdown, argument, command.Path);
        }

        foreach (var option in command.DeclaredOptions.Select(item => new ResolvedOption
                 {
                     Option = item,
                     IsInherited = false,
                 }).Concat(command.InheritedOptions))
        {
            AssertOptionSurface(markdown, option, command.Path);
        }

        foreach (var example in command.Command.Examples)
        {
            Assert.Contains(example, markdown);
        }

        foreach (var exitCode in command.Command.ExitCodes)
        {
            Assert.Contains(exitCode.Code.ToString(), markdown);
            AssertContainsIfPresent(markdown, exitCode.Description);
        }

        foreach (var metadata in command.Command.Metadata)
        {
            Assert.Contains(metadata.Name, markdown);
        }
    }

    private static void AssertArgumentSurface(string markdown, OpenCliArgument argument, string owner)
    {
        Assert.Contains(argument.Name, markdown);
        AssertContainsIfPresent(markdown, argument.Description);
        AssertContainsIfPresent(markdown, argument.Group);

        foreach (var value in argument.AcceptedValues)
        {
            Assert.Contains(value, markdown);
        }

        foreach (var metadata in argument.Metadata)
        {
            Assert.Contains(metadata.Name, markdown);
        }
    }

    private static void AssertOptionSurface(string markdown, ResolvedOption resolvedOption, string owner)
    {
        var option = resolvedOption.Option;
        Assert.Contains(option.Name, markdown);
        AssertContainsIfPresent(markdown, option.Description);
        AssertContainsIfPresent(markdown, option.Group);

        foreach (var alias in option.Aliases)
        {
            Assert.Contains(alias, markdown);
        }

        foreach (var argument in option.Arguments)
        {
            Assert.Contains(argument.Name, markdown);
            AssertContainsIfPresent(markdown, argument.Description);
            AssertContainsIfPresent(markdown, argument.Group);
            foreach (var value in argument.AcceptedValues)
            {
                Assert.Contains(value, markdown);
            }
        }

        if (resolvedOption.IsInherited)
        {
            Assert.Contains(resolvedOption.InheritedFromPath!, markdown);
        }

        foreach (var metadata in option.Metadata)
        {
            Assert.Contains(metadata.Name, markdown);
        }
    }

    private static void AssertContainsIfPresent(string markdown, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            Assert.Contains(value, markdown);
        }
    }
}
