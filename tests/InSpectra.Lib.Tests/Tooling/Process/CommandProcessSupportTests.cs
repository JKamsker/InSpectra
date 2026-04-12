namespace InSpectra.Lib.Tests.Tooling.Process;

using System.Diagnostics;
using InSpectra.Lib.Tooling.Process;
using InSpectra.Lib.Tests.TestSupport;

public sealed class CommandProcessSupportTests
{
    [Fact]
    public void IsWithinSandboxRoot_Returns_True_For_Child_Path()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var sandboxRoot = Path.Combine(tempDirectory.Path, "sandbox");
        Directory.CreateDirectory(sandboxRoot);
        var executablePath = Path.Combine(sandboxRoot, "tool", "demo.exe");

        Assert.True(CommandProcessSupport.IsWithinSandboxRoot(sandboxRoot, executablePath));
        Assert.True(CommandProcessSupport.IsWithinSandboxRoot(sandboxRoot + Path.DirectorySeparatorChar, executablePath));
    }

    [Fact]
    public void IsWithinSandboxRoot_Returns_False_For_Sibling_Prefix_Path()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var sandboxRoot = Path.Combine(tempDirectory.Path, "sandbox");
        Directory.CreateDirectory(sandboxRoot);
        var siblingPath = Path.Combine(tempDirectory.Path, "sandbox-old", "tool", "demo.exe");

        Assert.False(CommandProcessSupport.IsWithinSandboxRoot(sandboxRoot, siblingPath));
        Assert.False(CommandProcessSupport.IsWithinSandboxRoot(sandboxRoot + Path.DirectorySeparatorChar, siblingPath));
    }

    [Fact]
    public void IsWithinSandboxRoot_Returns_False_For_Filesystem_Roots()
    {
        var filesystemRoot = Path.GetPathRoot(Path.GetTempPath())!;
        var executablePath = Path.Combine(filesystemRoot, "inspectra-process-support", "demo.exe");

        Assert.False(CommandProcessSupport.IsWithinSandboxRoot(filesystemRoot, executablePath));
    }

    [Fact]
    public void IsWithinSandboxRoot_Respects_Case_Sensitive_Filesystems()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var sandboxRoot = Path.Combine(tempDirectory.Path, "sandbox");
        var executablePath = Path.Combine(tempDirectory.Path, "Sandbox", "tool", "demo");

        Assert.False(CommandProcessSupport.IsWithinSandboxRoot(sandboxRoot, executablePath));
    }

    [Fact]
    public void MatchesSandboxProcess_Returns_True_For_Dotnet_Hosted_Sandbox_Command()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var sandboxRoot = Path.Combine(tempDirectory.Path, "sandbox");
        var entryAssemblyPath = Path.Combine(sandboxRoot, "tool", "Demo.Tool.dll");
        var dotnetPath = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.SystemDirectory, "dotnet.exe")
            : "/usr/bin/dotnet";
        var commandLine = $"dotnet \"{entryAssemblyPath}\" --help";

        Assert.True(CommandProcessSupport.MatchesSandboxProcess(sandboxRoot, dotnetPath, commandLine));
    }

    [Fact]
    public void MatchesSandboxProcess_Returns_False_For_Dotnet_Host_Outside_The_Sandbox()
    {
        using var tempDirectory = new RepositoryRegressionTestSupport.TemporaryDirectory();
        var sandboxRoot = Path.Combine(tempDirectory.Path, "sandbox");
        var entryAssemblyPath = Path.Combine(tempDirectory.Path, "outside", "Demo.Tool.dll");
        var dotnetPath = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.SystemDirectory, "dotnet.exe")
            : "/usr/bin/dotnet";
        var commandLine = $"dotnet \"{entryAssemblyPath}\" --help";

        Assert.False(CommandProcessSupport.MatchesSandboxProcess(sandboxRoot, dotnetPath, commandLine));
    }

    [Fact]
    public async Task InvokeProcessCaptureAsync_TimedOutProcess_Always_Cleans_Up_Sandbox()
    {
        using var process = CreateLongRunningProcess();
        var cleanupRoots = new List<string?>();

        var result = await CommandProcessSupport.InvokeProcessCaptureAsync(
            process,
            timeoutSeconds: 1,
            sandboxRoot: "sandbox-root",
            cancellationToken: CancellationToken.None,
            terminateSandboxProcesses: cleanupRoots.Add);

        Assert.True(result.TimedOut);
        Assert.Equal("timed-out", result.Status);
        Assert.Equal(["sandbox-root"], cleanupRoots);
    }

    private static Process CreateLongRunningProcess()
    {
        var startInfo = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -n 6 >nul")
            : new ProcessStartInfo("/bin/sh", "-c \"sleep 5\"");

        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        return new Process { StartInfo = startInfo };
    }
}
