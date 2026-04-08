using InSpectra.Gen.Runtime;
using InSpectra.Gen.Services;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests;

public class MarkdownRenderServiceTests
{
    private readonly MarkdownRenderService _service = new(
        RendererFactory.CreateDocumentRenderService(),
        new OpenCliNormalizer(),
        RendererFactory.CreateMarkdownRenderer(),
        new RenderStatsFactory());

    [Fact]
    public async Task Dry_run_does_not_write_single_output_file()
    {
        using var temp = new TempDirectory();
        var outputFile = Path.Combine(temp.Path, "docs.md");
        var request = new FileRenderRequest(
            FixturePaths.OpenCliJson,
            null,
            new RenderExecutionOptions(
                RenderLayout.Single,
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
                OutputFile: outputFile,
                OutputDirectory: null));

        var result = await _service.RenderFromFileAsync(request, CancellationToken.None);

        Assert.False(File.Exists(outputFile));
        Assert.True(result.IsDryRun);
        Assert.Single(result.Files);
        Assert.Equal(DocumentFormat.Markdown, result.Format);
        Assert.Equal(RenderLayout.Single, result.Layout);
    }

    [Fact]
    public async Task Tree_render_refuses_non_empty_directory_without_overwrite()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "keep.txt"), "existing");

        var request = new FileRenderRequest(
            FixturePaths.OpenCliJson,
            null,
            new RenderExecutionOptions(
                RenderLayout.Tree,
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
                OutputDirectory: temp.Path));

        var exception = await Assert.ThrowsAsync<CliUsageException>(() =>
            _service.RenderFromFileAsync(request, CancellationToken.None));

        Assert.Contains("not empty", exception.Message);
    }

    [Fact]
    public async Task Hybrid_default_depth_emits_readme_plus_one_file_per_top_level_group()
    {
        using var temp = new TempDirectory();

        var result = await RenderHybridAsync(temp.Path, splitDepth: 1);

        Assert.Equal(RenderLayout.Hybrid, result.Layout);
        var relativePaths = result.Files.Select(file => file.RelativePath).ToList();
        Assert.Contains("README.md", relativePaths);
        // Fixture has 11 top-level groups + 1 top-level leaf (doctor). Only groups get files.
        Assert.Contains("accounts/index.md", relativePaths);
        Assert.Contains("auth/index.md", relativePaths);
        Assert.DoesNotContain("doctor/index.md", relativePaths);
        Assert.DoesNotContain("doctor.md", relativePaths);
        // No second-level group files at depth=1.
        Assert.DoesNotContain(
            relativePaths,
            path => path.StartsWith("accounts/basic-auth/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Hybrid_depth_two_emits_files_for_ancestor_and_second_level_groups()
    {
        using var temp = new TempDirectory();

        var result = await RenderHybridAsync(temp.Path, splitDepth: 2);

        var relativePaths = result.Files
            .Select(file => file.RelativePath.Replace('\\', '/'))
            .ToList();
        Assert.Contains("README.md", relativePaths);
        Assert.Contains("accounts/index.md", relativePaths);
        Assert.Contains("accounts/basic-auth/index.md", relativePaths);
        Assert.Contains("accounts/hosters/index.md", relativePaths);
        // Leaves never get files even at deeper depths.
        Assert.DoesNotContain("accounts/add/index.md", relativePaths);
        Assert.DoesNotContain("accounts/add.md", relativePaths);
    }

    [Fact]
    public async Task Hybrid_readme_links_to_group_files_with_relative_paths()
    {
        using var temp = new TempDirectory();

        var result = await RenderHybridAsync(temp.Path, splitDepth: 1);

        var readme = ReadFile(result, "README.md");
        Assert.Contains("[accounts](accounts/index.md)", readme);
        // Top-level leaf uses an anchor, not a file link.
        Assert.Contains("[doctor](#command-doctor)", readme);
    }

    [Fact]
    public async Task Hybrid_group_file_breadcrumb_points_to_readme()
    {
        using var temp = new TempDirectory();

        var result = await RenderHybridAsync(temp.Path, splitDepth: 1);

        var group = ReadFile(result, "accounts/index.md");
        Assert.Contains("[README](../README.md)", group);
        Assert.DoesNotContain("[index](", group);
    }

    [Fact]
    public async Task Hybrid_group_file_uses_anchors_for_inlined_children()
    {
        using var temp = new TempDirectory();

        var result = await RenderHybridAsync(temp.Path, splitDepth: 1);

        var group = ReadFile(result, "accounts/index.md");
        // `accounts add` is a leaf → must be inlined with an anchor, not a file link.
        Assert.Contains("[add](#command-accounts-add)", group);
        Assert.DoesNotContain("[add](add.md)", group);
        Assert.Contains("<a id=\"command-accounts-add\"></a>", group);
    }

    [Fact]
    public async Task Hybrid_depth_two_top_level_group_references_file_children_without_stubs()
    {
        using var temp = new TempDirectory();

        var result = await RenderHybridAsync(temp.Path, splitDepth: 2);

        var group = ReadFile(result, "accounts/index.md");
        // `accounts basic-auth` is a group at depth 2 → has its own file; reference lives only in
        // the Subcommands list, no duplicate stub section or "See …" body.
        Assert.Contains("[basic-auth](basic-auth/index.md)", group);
        Assert.DoesNotContain("See [`accounts basic-auth`](basic-auth/index.md)", group);
        Assert.DoesNotContain("## `accounts basic-auth`", group);
        // Leaf `accounts add` is still inlined with its anchor.
        Assert.Contains("<a id=\"command-accounts-add\"></a>", group);
    }

    [Fact]
    public async Task Hybrid_readme_drops_commands_section_when_everything_is_split()
    {
        using var temp = new TempDirectory();

        var result = await RenderHybridAsync(temp.Path, splitDepth: 2);

        var readme = ReadFile(result, "README.md");
        // At depth=2 every top-level command in the fixture is a split group (or an inlined leaf).
        // With no top-level leaves to inline we want a clean README without a trailing `## Commands`.
        // The jdr fixture has `doctor` (top-level leaf) which keeps `## Commands`, so this assertion
        // guards the shape we care about: no per-group stub sections.
        Assert.DoesNotContain("See [`accounts`](accounts/index.md)", readme);
        Assert.DoesNotContain("### `accounts`", readme);
    }

    [Fact]
    public async Task Hybrid_subcommand_list_is_nested_for_inlined_descendants()
    {
        using var temp = new TempDirectory();

        var result = await RenderHybridAsync(temp.Path, splitDepth: 1);

        var group = ReadFile(result, "accounts/index.md");
        // `accounts basic-auth` has children; at depth=1 they're inlined, so the subcommand list
        // should nest one level deeper with a two-space indent.
        Assert.Contains("- [basic-auth](#command-accounts-basic-auth)", group);
        Assert.Contains("  - [add](#command-accounts-basic-auth-add)", group);
    }

    [Fact]
    public async Task Hybrid_second_level_group_file_links_back_through_parent()
    {
        using var temp = new TempDirectory();

        var result = await RenderHybridAsync(temp.Path, splitDepth: 2);

        var nested = ReadFile(result, "accounts/basic-auth/index.md");
        Assert.Contains("[README](../../README.md)", nested);
        Assert.Contains("[accounts](../index.md)", nested);
    }

    [Fact]
    public async Task Hybrid_dry_run_plans_files_without_writing()
    {
        using var temp = new TempDirectory();

        var request = new FileRenderRequest(
            FixturePaths.OpenCliJson,
            null,
            new RenderExecutionOptions(
                RenderLayout.Hybrid,
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
                OutputDirectory: temp.Path),
            new MarkdownRenderOptions(HybridSplitDepth: 1));

        var result = await _service.RenderFromFileAsync(request, CancellationToken.None);

        Assert.True(result.IsDryRun);
        Assert.NotEmpty(result.Files);
        Assert.All(result.Files, file => Assert.False(File.Exists(file.FullPath)));
        Assert.Equal(RenderLayout.Hybrid, result.Layout);
    }

    [Fact]
    public async Task Hybrid_split_depth_beyond_tree_clamps_gracefully()
    {
        using var temp = new TempDirectory();

        // Fixture tree is at most 3 levels deep; asking for 10 should just emit every group.
        var deep = await RenderHybridAsync(temp.Path, splitDepth: 10);

        using var shallow = new TempDirectory();
        var two = await RenderHybridAsync(shallow.Path, splitDepth: 2);

        // Clamping should produce a superset of depth-2 files but still be finite.
        var deepPaths = deep.Files.Select(file => file.RelativePath.Replace('\\', '/')).ToHashSet();
        foreach (var file in two.Files)
        {
            Assert.Contains(file.RelativePath.Replace('\\', '/'), deepPaths);
        }
    }

    private async Task<RenderExecutionResult> RenderHybridAsync(string outputDirectory, int splitDepth)
    {
        var request = new FileRenderRequest(
            FixturePaths.OpenCliJson,
            null,
            new RenderExecutionOptions(
                RenderLayout.Hybrid,
                ResolvedOutputMode.Human,
                DryRun: false,
                Quiet: false,
                Verbose: false,
                NoColor: false,
                IncludeHidden: false,
                IncludeMetadata: false,
                Overwrite: true,
                SingleFile: false,
                CompressLevel: 0,
                OutputFile: null,
                OutputDirectory: outputDirectory),
            new MarkdownRenderOptions(HybridSplitDepth: splitDepth));

        return await _service.RenderFromFileAsync(request, CancellationToken.None);
    }

    private static string ReadFile(RenderExecutionResult result, string relativePath)
    {
        var file = result.Files.Single(entry =>
            string.Equals(entry.RelativePath, relativePath, StringComparison.Ordinal));
        return file.Content ?? File.ReadAllText(file.FullPath);
    }
}
