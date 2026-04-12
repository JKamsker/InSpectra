using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using InSpectra.Gen.Commands.Render;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests;

public class CliHelpSurfaceTests
{
    [Fact]
    public async Task Render_help_only_exposes_file_branch()
    {
        var result = await RunRendererAsync(["render", "--help"]);
        var helpText = NormalizeHelpOutput(result.StandardOutput);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("file", helpText);
        Assert.DoesNotContain("exec", helpText);
        Assert.DoesNotContain("dotnet", helpText);
        Assert.DoesNotContain("package", helpText);
        Assert.DoesNotContain("self", helpText);
    }

    [Fact]
    public async Task Version_command_uses_the_assembly_version()
    {
        var result = await RunRendererAsync(["--version"]);
        var reportedVersion = result.StandardOutput.Trim();
        var assemblyVersion = typeof(RenderRequestFactory).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?.Split('+')[0];

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(assemblyVersion, reportedVersion);
    }

    [Fact]
    public async Task Html_help_only_exposes_bundle_output_option()
    {
        var result = await RunRendererAsync(["render", "file", "html", "--help"]);
        var helpText = NormalizeHelpOutput(result.StandardOutput);

        Assert.Equal(0, result.ExitCode);
        AssertOption(helpText, "--out-dir", "<DIR>");
        AssertNoOption(helpText, "--out", "<FILE>");
        AssertNoOption(helpText, "--layout", "<LAYOUT>");
    }

    [Fact]
    public async Task Markdown_help_keeps_single_and_tree_output_options()
    {
        var result = await RunRendererAsync(["render", "file", "markdown", "--help"]);
        var helpText = NormalizeHelpOutput(result.StandardOutput);

        Assert.Equal(0, result.ExitCode);
        AssertOption(helpText, "--layout", "<LAYOUT>");
        AssertOption(helpText, "--out", "<FILE>");
        AssertOption(helpText, "--out-dir", "<DIR>");
    }

    [Theory]
    [InlineData("exec")]
    [InlineData("dotnet")]
    [InlineData("package")]
    public async Task Generate_help_exposes_generate_output_options_only(string mode)
    {
        var result = await RunRendererAsync(["generate", mode, "--help"]);
        var helpText = NormalizeHelpOutput(result.StandardOutput);

        Assert.Equal(0, result.ExitCode);
        AssertOption(helpText, "--out", "<FILE>");
        AssertNoOption(helpText, "--out-dir", "<DIR>");
        AssertNoOption(helpText, "--layout", "<LAYOUT>");
        AssertOption(helpText, "--opencli-mode", "<MODE>");
        AssertOption(helpText, "--with-xmldoc", string.Empty);
        AssertOption(helpText, "--xmldoc-arg", "<ARG>");
        AssertOption(helpText, "--crawl-out", "<PATH>");
        AssertOption(helpText, "--overwrite", string.Empty);
        AssertNoOption(helpText, "--quiet", string.Empty);
        AssertNoOption(helpText, "--no-color", string.Empty);
    }

    private static async Task<ProcessResult> RunRendererAsync(IReadOnlyList<string> arguments)
    {
        var dllPath = ResolveBuiltRendererPath();

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = FixturePaths.RepoRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.ArgumentList.Add(dllPath);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["NO_COLOR"] = "1";

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);

        await process.WaitForExitAsync(timeout.Token);

        return new ProcessResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask);
    }

    private static string ResolveBuiltRendererPath()
    {
        var candidatePaths = new[]
        {
            Path.Combine(FixturePaths.RepoRoot, "src", "InSpectra.Gen", "bin", "Debug", "net10.0", "inspectra.dll"),
            Path.Combine(FixturePaths.RepoRoot, "src", "InSpectra.Gen", "bin", "Release", "net10.0", "inspectra.dll"),
        };

        var existingPath = candidatePaths.FirstOrDefault(File.Exists);
        return existingPath
            ?? throw new FileNotFoundException("Could not locate a built inspectra.dll.", candidatePaths[0]);
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    private static string NormalizeHelpOutput(string helpText)
    {
        return Regex.Replace(
            helpText,
            @"\x1B\[[0-9;?]*[ -/]*[@-~]",
            string.Empty);
    }

    private static void AssertOption(string helpText, string optionName, string argumentName)
    {
        Assert.Matches(BuildOptionPattern(optionName, argumentName), helpText);
    }

    private static void AssertNoOption(string helpText, string optionName, string argumentName)
    {
        Assert.DoesNotMatch(BuildOptionPattern(optionName, argumentName), helpText);
    }

    private static Regex BuildOptionPattern(string optionName, string argumentName)
    {
        if (string.IsNullOrEmpty(argumentName))
        {
            return new Regex(
                $@"(?<!\S){Regex.Escape(optionName)}(?!\S)",
                RegexOptions.CultureInvariant);
        }

        return new Regex(
            $@"(?<!\S){Regex.Escape(optionName)}\s+{Regex.Escape(argumentName)}(?!\S)",
            RegexOptions.CultureInvariant);
    }

}
