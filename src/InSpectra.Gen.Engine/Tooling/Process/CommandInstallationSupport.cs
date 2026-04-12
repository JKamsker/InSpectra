namespace InSpectra.Gen.Engine.Tooling.Process;

using InSpectra.Gen.Engine.Tooling.Paths;

using InSpectra.Gen.Engine.Tooling.Results;

using InSpectra.Gen.Engine.Contracts;
using System.Text.Json.Nodes;
using System.Text;

internal static class CommandInstallationSupport
{
    public static async Task<InstalledToolContext?> InstallToolAsync(
        CommandRuntime runtime,
        JsonObject result,
        string packageId,
        string version,
        string commandName,
        string tempRoot,
        int installTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        var sandbox = runtime.CreateSandboxEnvironment(tempRoot);
        EnsureDirectories(sandbox.Directories);

        var installDirectory = Path.Combine(tempRoot, "tool");
        var installResult = await runtime.InvokeProcessCaptureAsync(
            DotnetHostPathResolutionSupport.ResolveDotnetHostPath(),
            ["tool", "install", packageId, "--version", version, "--tool-path", installDirectory],
            tempRoot,
            sandbox.Values,
            installTimeoutSeconds,
            tempRoot,
            cancellationToken);

        result["steps"]!.AsObject()["install"] = installResult.ToJsonObject();
        result["timings"]!.AsObject()["installMs"] = installResult.DurationMs;

        if (installResult.TimedOut || installResult.ExitCode != 0)
        {
            NonSpectreResultSupport.ApplyRetryableFailure(
                result,
                phase: "install",
                classification: installResult.TimedOut ? "install-timeout" : "install-failed",
                BuildInstallFailureMessage(installResult));
            return null;
        }

        var commandPath = runtime.ResolveInstalledCommandPath(installDirectory, commandName);
        if (commandPath is null)
        {
            NonSpectreResultSupport.ApplyRetryableFailure(
                result,
                phase: "install",
                classification: "installed-command-missing",
                $"Installed tool command '{commandName}' was not found.");
            return null;
        }

        return new InstalledToolContext(
            Environment: sandbox.Values,
            InstallDirectory: installDirectory,
            CommandPath: commandPath,
            PreferredEntryPointPath: InstalledDotnetToolCommandSupport.TryResolve(installDirectory, commandName)?.EntryPointPath,
            CleanupRoot: sandbox.CleanupRoot);
    }

    private static void EnsureDirectories(IReadOnlyList<string> directories)
    {
        foreach (var directory in directories)
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string BuildInstallFailureMessage(CommandRuntime.ProcessResult installResult)
    {
        var stdout = CommandRuntime.NormalizeConsoleText(installResult.Stdout);
        var stderr = CommandRuntime.NormalizeConsoleText(installResult.Stderr);
        if (string.IsNullOrWhiteSpace(stdout) && string.IsNullOrWhiteSpace(stderr))
        {
            return "Tool installation failed.";
        }

        var builder = new StringBuilder("Tool installation failed.");
        AppendDiagnostic(builder, "stdout", stdout);
        AppendDiagnostic(builder, "stderr", stderr);
        return builder.ToString();
    }

    private static void AppendDiagnostic(StringBuilder builder, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.Append(' ');
        builder.Append(label);
        builder.Append(": ");
        builder.Append(value);
    }
}
