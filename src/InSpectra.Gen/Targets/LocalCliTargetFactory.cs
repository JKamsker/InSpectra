using System.Diagnostics;
using System.Reflection;
using InSpectra.Gen.Acquisition.Contracts.Exceptions;

namespace InSpectra.Gen.Targets;

public sealed class LocalCliTargetFactory(LocalCliFrameworkDetector frameworkDetector)
{
    internal MaterializedCliTarget Create(
        string sourcePath,
        IReadOnlyList<string> sourceArguments,
        string workingDirectory,
        string stagingDirectory,
        string? commandName,
        string? cliFramework)
    {
        var resolvedSourcePath = Path.GetFullPath(sourcePath);
        var installDirectory = Path.GetDirectoryName(resolvedSourcePath)
            ?? throw new CliUsageException($"Could not resolve the directory for `{resolvedSourcePath}`.");
        var effectiveCommandName = string.IsNullOrWhiteSpace(commandName)
            ? Path.GetFileNameWithoutExtension(resolvedSourcePath)
            : commandName.Trim();
        var preferredEntryPointPath = ResolvePreferredEntryPointPath(resolvedSourcePath);
        var version = ResolveVersion(preferredEntryPointPath ?? resolvedSourcePath);
        var detection = frameworkDetector.Detect(installDirectory);
        var effectiveCliFramework = string.IsNullOrWhiteSpace(cliFramework)
            ? detection.CliFramework
            : cliFramework.Trim();
        var hookCliFramework = string.IsNullOrWhiteSpace(cliFramework)
            ? detection.HookCliFramework
            : cliFramework.Trim();
        var commandPath = RequiresWrapper(resolvedSourcePath, sourceArguments)
            ? CreateWrapper(stagingDirectory, resolvedSourcePath, sourceArguments)
            : resolvedSourcePath;

        return new MaterializedCliTarget(
            DisplayName: resolvedSourcePath,
            CommandPath: commandPath,
            CommandName: effectiveCommandName,
            WorkingDirectory: workingDirectory,
            InstallDirectory: installDirectory,
            PreferredEntryPointPath: preferredEntryPointPath,
            Version: version,
            Environment: CliInvocationEnvironmentFactory.CreateCurrentProcessSnapshot(),
            CliFramework: effectiveCliFramework,
            HookCliFramework: hookCliFramework);
    }

    private static bool RequiresWrapper(string sourcePath, IReadOnlyList<string> sourceArguments)
        => sourceArguments.Count > 0
            || string.Equals(Path.GetExtension(sourcePath), ".dll", StringComparison.OrdinalIgnoreCase);

    private static string CreateWrapper(
        string stagingDirectory,
        string sourcePath,
        IReadOnlyList<string> sourceArguments)
    {
        Directory.CreateDirectory(stagingDirectory);
        var wrapperPath = Path.Combine(
            stagingDirectory,
            OperatingSystem.IsWindows() ? "inspectra-local-target.cmd" : "inspectra-local-target.sh");

        var fileName = string.Equals(Path.GetExtension(sourcePath), ".dll", StringComparison.OrdinalIgnoreCase)
            ? "dotnet"
            : sourcePath;
        var prefixArguments = string.Equals(Path.GetExtension(sourcePath), ".dll", StringComparison.OrdinalIgnoreCase)
            ? new[] { sourcePath }.Concat(sourceArguments).ToArray()
            : sourceArguments.ToArray();

        var content = OperatingSystem.IsWindows()
            ? BuildWindowsWrapper(fileName, prefixArguments)
            : BuildPosixWrapper(fileName, prefixArguments);

        File.WriteAllText(wrapperPath, content);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                wrapperPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        return wrapperPath;
    }

    private static string BuildWindowsWrapper(string fileName, IReadOnlyList<string> prefixArguments)
    {
        var prefix = string.Join(' ', prefixArguments.Select(EscapeForCmdArgument));
        return string.IsNullOrWhiteSpace(prefix)
            ? $"@echo off{Environment.NewLine}\"{fileName}\" %*{Environment.NewLine}"
            : $"@echo off{Environment.NewLine}\"{fileName}\" {prefix} %*{Environment.NewLine}";
    }

    private static string BuildPosixWrapper(string fileName, IReadOnlyList<string> prefixArguments)
    {
        var prefix = string.Join(' ', prefixArguments.Select(EscapeForPosixArgument));
        return string.IsNullOrWhiteSpace(prefix)
            ? $"#!/usr/bin/env sh{Environment.NewLine}\"{fileName}\" \"$@\"{Environment.NewLine}"
            : $"#!/usr/bin/env sh{Environment.NewLine}\"{fileName}\" {prefix} \"$@\"{Environment.NewLine}";
    }

    private static string EscapeForCmdArgument(string value)
        => $"\"{value.Replace("%", "%%", StringComparison.Ordinal).Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static string EscapeForPosixArgument(string value)
        => $"'{value.Replace("'", "'\"'\"'", StringComparison.Ordinal)}'";

    private static string? ResolvePreferredEntryPointPath(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        return extension switch
        {
            ".dll" or ".exe" => sourcePath,
            _ => null,
        };
    }

    private static string ResolveVersion(string sourcePath)
    {
        try
        {
            var fileVersion = FileVersionInfo.GetVersionInfo(sourcePath);
            if (!string.IsNullOrWhiteSpace(fileVersion.ProductVersion))
            {
                return fileVersion.ProductVersion!;
            }

            if (!string.IsNullOrWhiteSpace(fileVersion.FileVersion))
            {
                return fileVersion.FileVersion!;
            }
        }
        catch
        {
        }

        try
        {
            var assemblyName = AssemblyName.GetAssemblyName(sourcePath);
            if (assemblyName.Version is not null)
            {
                return assemblyName.Version.ToString();
            }
        }
        catch
        {
        }

        return "0.0.0-local";
    }
}
