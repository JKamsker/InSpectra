using System.Diagnostics;
using InSpectra.Gen.Core;
using Microsoft.Extensions.Options;

namespace InSpectra.Gen.Rendering.Html.Bundle;

public class ViewerBundleLocator(IOptions<ViewerBundleLocatorOptions> options)
    : IViewerBundleLocator
{
    private const string FrontendBuildHint = "Run `npm ci` and `npm run build` in `src/InSpectra.UI` to build the viewer bundle.";
    private static readonly string[] FrontendInputFiles =
    [
        "index.html",
        "static.html",
        "package.json",
        "package-lock.json",
        "tsconfig.json",
        "vite.config.ts",
    ];

    public async Task<string> ResolveAsync(CancellationToken cancellationToken, bool allowBuild = true)
    {
        var packagedPath = options.Value.PackagedRootPath ?? Path.Combine(AppContext.BaseDirectory, "InSpectra.UI", "dist");
        var repositoryRoot = options.Value.RepositoryRootPath ?? FindRepositoryRoot();
        var frontendRoot = repositoryRoot is null ? null : Path.Combine(repositoryRoot, "src", "InSpectra.UI");

        var hasPackagedBundle = HasBundle(packagedPath);
        if (hasPackagedBundle)
        {
            return await ResolvePackagedBundleAsync(packagedPath, frontendRoot, cancellationToken, allowBuild);
        }

        if (frontendRoot is not null && HasFrontendProject(frontendRoot))
            return await ResolveRepositoryBundleAsync(frontendRoot, cancellationToken, allowBuild);

        throw new CliUsageException($"InSpectra.UI bundle could not be located beside the tool. {FrontendBuildHint}");
    }

    protected virtual async Task BuildBundleAsync(string frontendRoot, string repositoryDist, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(frontendRoot))
        {
            throw new CliUsageException($"InSpectra.UI sources were not found at `{frontendRoot}`. {FrontendBuildHint}");
        }

        var packageJsonPath = Path.Combine(frontendRoot, "package.json");
        var packageLockPath = Path.Combine(frontendRoot, "package-lock.json");
        if (!File.Exists(packageJsonPath) || !File.Exists(packageLockPath))
        {
            throw new CliUsageException($"InSpectra.UI package metadata is missing in `{frontendRoot}`. {FrontendBuildHint}");
        }

        var npmExecutable = ResolveNpmExecutable(frontendRoot);
        switch (GetNodeModulesState(frontendRoot))
        {
            case NodeModulesState.Missing:
                await RunProcessAsync(npmExecutable, frontendRoot, repositoryDist, ["ci"], cancellationToken);
                break;
            case NodeModulesState.Incomplete:
                await RunProcessAsync(npmExecutable, frontendRoot, repositoryDist, ["install"], cancellationToken);
                break;
        }

        await RunProcessAsync(npmExecutable, frontendRoot, repositoryDist, ["run", "build"], cancellationToken);
    }

    private static string? FindRepositoryRoot()
    {
        foreach (var current in CandidateDirectories().Select(start => new DirectoryInfo(start)))
        {
            for (var directory = current; directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "InSpectra.Gen.sln")))
                {
                    return directory.FullName;
                }
            }
        }
        return null;
    }

    private static IEnumerable<string> CandidateDirectories() => [AppContext.BaseDirectory, Directory.GetCurrentDirectory()];
    private static bool HasBundle(string path) => File.Exists(Path.Combine(path, "static.html"));

    private string ResolveNpmExecutable(string frontendRoot)
    {
        var configuredExecutable = options.Value.NpmExecutablePath;
        if (!string.IsNullOrWhiteSpace(configuredExecutable))
        {
            return Path.IsPathRooted(configuredExecutable) || ContainsDirectorySeparator(configuredExecutable)
                ? Path.GetFullPath(configuredExecutable, frontendRoot)
                : configuredExecutable;
        }

        foreach (var directory in EnumerateSearchDirectories(frontendRoot))
        {
            var match = ResolveFromDirectory(directory, "npm");
            if (match is not null) return match;
        }
        throw new CliUsageException($"InSpectra.UI dist was not found and `npm` is not available on PATH. {FrontendBuildHint}");
    }

    private static IEnumerable<string> EnumerateSearchDirectories(string workingDirectory)
    {
        yield return workingDirectory;
        foreach (var pathEntry in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            yield return pathEntry;
    }

    private static string? ResolveFromDirectory(string directory, string executableName)
    {
        if (!Directory.Exists(directory)) return null;
        var exactPath = Path.Combine(directory, executableName);
        if (Path.HasExtension(executableName)) return File.Exists(exactPath) ? exactPath : null;

        if (OperatingSystem.IsWindows())
        {
            foreach (var extension in (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM")
                         .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (File.Exists(exactPath + extension.ToLowerInvariant())) return exactPath + extension.ToLowerInvariant();
                if (File.Exists(exactPath + extension.ToUpperInvariant())) return exactPath + extension.ToUpperInvariant();
            }
        }

        return File.Exists(exactPath) ? exactPath : null;
    }

    private static bool ContainsDirectorySeparator(string value)
    {
        return value.Contains(Path.DirectorySeparatorChar) || value.Contains(Path.AltDirectorySeparatorChar);
    }

    private static NodeModulesState GetNodeModulesState(string frontendRoot)
    {
        var nodeModulesPath = Path.Combine(frontendRoot, "node_modules");
        if (!Directory.Exists(nodeModulesPath)) return NodeModulesState.Missing;
        var toolsPath = Path.Combine(nodeModulesPath, ".bin");
        return Directory.Exists(toolsPath) ? NodeModulesState.Ready : NodeModulesState.Incomplete;
    }

    private async Task<string> ResolveRepositoryBundleAsync(string frontendRoot, CancellationToken cancellationToken, bool allowBuild)
    {
        var repositoryDist = Path.Combine(frontendRoot, "dist");
        if (HasBundle(repositoryDist))
        {
            if (!allowBuild || !IsBundleStale(frontendRoot, repositoryDist))
            {
                return repositoryDist;
            }
        }

        if (!allowBuild)
        {
            throw new CliUsageException($"InSpectra.UI bundle could not be located beside the tool. {FrontendBuildHint}");
        }

        await BuildBundleAsync(frontendRoot, repositoryDist, cancellationToken);
        if (HasBundle(repositoryDist))
            return repositoryDist;

        throw new CliUsageException($"InSpectra.UI bundle is missing after the build attempt. {FrontendBuildHint}");
    }

    private async Task<string> ResolvePackagedBundleAsync(
        string packagedPath,
        string? frontendRoot,
        CancellationToken cancellationToken,
        bool allowBuild)
    {
        if (frontendRoot is null || !HasFrontendProject(frontendRoot)) return packagedPath;
        var repositoryDist = Path.Combine(frontendRoot, "dist");
        if (!IsBundleStale(frontendRoot, packagedPath)) return packagedPath;

        if (!HasBundle(repositoryDist))
        {
            return allowBuild
                ? await ResolveRepositoryBundleOrUsePackagedAsync(packagedPath, frontendRoot, cancellationToken, allowBuild)
                : packagedPath;
        }

        if (!IsBundleStale(frontendRoot, repositoryDist)) return repositoryDist;
        if (!allowBuild) return SelectNewerBundle(packagedPath, repositoryDist);
        return await ResolveRepositoryBundleOrUsePackagedAsync(packagedPath, frontendRoot, cancellationToken, allowBuild);
    }

    private static bool HasFrontendProject(string frontendRoot) =>
        Directory.Exists(frontendRoot)
        && File.Exists(Path.Combine(frontendRoot, "package.json"))
        && File.Exists(Path.Combine(frontendRoot, "package-lock.json"));

    private async Task<string> ResolveRepositoryBundleOrUsePackagedAsync(
        string packagedPath,
        string frontendRoot,
        CancellationToken cancellationToken,
        bool allowBuild)
    {
        try
        {
            return await ResolveRepositoryBundleAsync(frontendRoot, cancellationToken, allowBuild);
        }
        catch (CliUsageException)
        {
            var repositoryDist = Path.Combine(frontendRoot, "dist");
            return HasBundle(repositoryDist) ? SelectNewerBundle(packagedPath, repositoryDist) : packagedPath;
        }
    }

    private static bool IsBundleStale(string frontendRoot, string bundleRoot)
    {
        var bundleWriteTime = GetLatestWriteTimeUtc(bundleRoot);
        foreach (var inputPath in EnumerateFrontendInputs(frontendRoot))
        {
            if (File.GetLastWriteTimeUtc(inputPath) > bundleWriteTime) return true;
        }
        return false;
    }

    private static IEnumerable<string> EnumerateFrontendInputs(string frontendRoot)
    {
        foreach (var relativePath in FrontendInputFiles)
        {
            var absolutePath = Path.Combine(frontendRoot, relativePath);
            if (File.Exists(absolutePath)) yield return absolutePath;
        }

        var sourceRoot = Path.Combine(frontendRoot, "src");
        if (!Directory.Exists(sourceRoot)) yield break;
        foreach (var sourcePath in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
            yield return sourcePath;
    }

    private static DateTime GetLatestWriteTimeUtc(string directoryPath) =>
        Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
            .Select(File.GetLastWriteTimeUtc)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();
    private static string SelectNewerBundle(string packagedPath, string repositoryDist)
        => GetLatestWriteTimeUtc(repositoryDist) > GetLatestWriteTimeUtc(packagedPath)
            ? repositoryDist
            : packagedPath;

    private async Task RunProcessAsync(
        string executablePath,
        string workingDirectory,
        string repositoryDist,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = CreateStartInfo(executablePath, workingDirectory) };
        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        try
        {
            process.Start();
        }
        catch (Exception exception)
        {
            throw CreateBuildFailure(workingDirectory, repositoryDist, $"Failed to start `{executablePath}`.", [exception.Message]);
        }

        process.StandardInput.Close();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.NpmTimeoutSeconds));

        try
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);

            await process.WaitForExitAsync(timeout.Token);
            _ = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                var details = new List<string> { $"Exit code: {process.ExitCode}" };
                if (!string.IsNullOrWhiteSpace(stderr)) details.Add(stderr.Trim());
                throw CreateBuildFailure(workingDirectory, repositoryDist, $"`{executablePath}` exited with code {process.ExitCode}.", details);
            }
        }
        catch (OperationCanceledException)
        {
            TryTerminate(process);
            if (cancellationToken.IsCancellationRequested) throw;
            throw CreateBuildFailure(
                workingDirectory,
                repositoryDist,
                $"`{executablePath}` did not finish within {options.Value.NpmTimeoutSeconds} seconds.",
                arguments.Count > 0 ? [$"Arguments: {string.Join(' ', arguments)}"] : []);
        }
    }

    private static ProcessStartInfo CreateStartInfo(string executablePath, string workingDirectory) => new()
    {
        FileName = executablePath,
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        RedirectStandardInput = true,
    };

    private static CliUsageException CreateBuildFailure(
        string frontendRoot,
        string repositoryDist,
        string message,
        IReadOnlyList<string>? details = null)
        => new(
            $"Failed to build InSpectra.UI in `{frontendRoot}`.",
            [message, .. details ?? [], $"Expected bundle path: `{repositoryDist}`."]);

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch { }
    }

    private enum NodeModulesState
    {
        Missing,
        Incomplete,
        Ready,
    }
}
