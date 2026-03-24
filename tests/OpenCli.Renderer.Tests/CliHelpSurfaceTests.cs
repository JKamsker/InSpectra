using System.Diagnostics;
using OpenCli.Renderer.Tests.TestSupport;

namespace OpenCli.Renderer.Tests;

public class CliHelpSurfaceTests
{
    [Theory]
    [InlineData("file")]
    [InlineData("exec")]
    public async Task Html_help_only_exposes_bundle_output_option(string mode)
    {
        var result = await RunRendererAsync(["render", mode, "html", "--help"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--out-dir <DIR>", result.StandardOutput);
        Assert.DoesNotContain("--out <FILE>", result.StandardOutput);
        Assert.DoesNotContain("--layout <LAYOUT>", result.StandardOutput);
    }

    private static async Task<ProcessResult> RunRendererAsync(IReadOnlyList<string> arguments)
    {
        var dllPath = Path.Combine(
            FixturePaths.RepoRoot,
            "src",
            "OpenCli.Renderer",
            "bin",
            "Release",
            "net10.0",
            "opencli-renderer.dll");

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

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
