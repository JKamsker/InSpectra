namespace InSpectra.Lib.Modes.Hook.Execution;

using InSpectra.Lib.Contracts.Signatures;
using InSpectra.Lib.Tooling.Process;

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json.Nodes;

internal static class HookToolProcessInvocationResolver
{
    public static HookToolProcessInvocationResolution Resolve(
        string installDirectory,
        string commandName,
        string commandPath,
        string? preferredEntryPointPath = null)
        => TryResolveDotnetRunnerInvocation(installDirectory, commandName)
            ?? HookToolProcessInvocationResolution.FromInvocation(
                new HookToolProcessInvocation(commandPath, ["--help"], preferredEntryPointPath ?? commandPath));

    public static string? TryResolvePreferredAssemblyDirectory(HookToolProcessInvocation invocation)
    {
        var candidatePath = !string.IsNullOrWhiteSpace(invocation.PreferredAssemblyPath)
            ? invocation.PreferredAssemblyPath
            : invocation.ArgumentList.Count > 0
                && string.Equals(Path.GetFileNameWithoutExtension(invocation.FilePath), "dotnet", StringComparison.OrdinalIgnoreCase)
                ? invocation.ArgumentList[0]
                : invocation.FilePath;
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            return null;
        }

        try
        {
            var fullPath = Path.GetFullPath(candidatePath);
            return Path.GetDirectoryName(fullPath);
        }
        catch
        {
            return null;
        }
    }

    public static IReadOnlyList<HookToolProcessInvocation> BuildHelpFallbackInvocations(HookToolProcessInvocation invocation)
    {
        if (invocation.ArgumentList.Count == 0
            || !IsHelpSwitch(invocation.ArgumentList[^1]))
        {
            return [];
        }

        var prefixLength = IsDotnetHostInvocation(invocation) ? 1 : 0;
        var prefixArguments = invocation.ArgumentList.Take(prefixLength).ToArray();
        var commandArguments = invocation.ArgumentList.Skip(prefixLength).ToArray();
        if (commandArguments.Length == 0 || !IsHelpSwitch(commandArguments[^1]))
        {
            return [];
        }

        var commandSegments = commandArguments[..^1];
        return InvocationSupport.BuildHelpInvocations(commandSegments)
            .Where(arguments => arguments.Length > 0)
            .Where(ContainsExplicitHelpRequest)
            .Where(arguments => !arguments.SequenceEqual(commandArguments, StringComparer.Ordinal))
            .Select(arguments =>
                new HookToolProcessInvocation(
                    invocation.FilePath,
                    [.. prefixArguments, .. arguments],
                    invocation.PreferredAssemblyPath))
            .ToArray();
    }

    private static bool IsDotnetHostInvocation(HookToolProcessInvocation invocation)
        => string.Equals(
            Path.GetFileNameWithoutExtension(invocation.FilePath),
            "dotnet",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsHelpSwitch(string argument)
        => string.Equals(argument, "--help", StringComparison.Ordinal)
            || string.Equals(argument, "-h", StringComparison.Ordinal)
            || string.Equals(argument, "-?", StringComparison.Ordinal)
            || string.Equals(argument, "--h", StringComparison.Ordinal)
            || string.Equals(argument, "/help", StringComparison.Ordinal)
            || string.Equals(argument, "/?", StringComparison.Ordinal);

    private static bool ContainsExplicitHelpRequest(IReadOnlyList<string> arguments)
        => arguments.Any(argument =>
            IsHelpSwitch(argument)
            || string.Equals(argument, "help", StringComparison.OrdinalIgnoreCase));

    private static HookToolProcessInvocationResolution? TryResolveDotnetRunnerInvocation(string installDirectory, string commandName)
    {
        var command = InstalledDotnetToolCommandSupport.TryResolve(installDirectory, commandName);
        if (command is null
            || !string.Equals(command.Runner, "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var entryPointPath = command.EntryPointPath;
        if (!File.Exists(entryPointPath))
        {
            return HookToolProcessInvocationResolution.TerminalFailure(
                "hook-invalid-dotnet-entrypoint",
                $"Dotnet tool entry point '{Path.GetFileName(entryPointPath)}' was declared in DotnetToolSettings.xml but was not found.");
        }

        if (!EnsureRuntimeConfig(entryPointPath, command.SettingsPath))
        {
            return null;
        }

        if (TryValidateManagedEntryPoint(entryPointPath, out var validationFailureMessage))
        {
            return HookToolProcessInvocationResolution.TerminalFailure(
                "hook-invalid-dotnet-entrypoint",
                validationFailureMessage);
        }

        return HookToolProcessInvocationResolution.FromInvocation(
            new HookToolProcessInvocation(ResolveDotnetHostPath(), [entryPointPath, "--help"], entryPointPath));
    }

    private static bool EnsureRuntimeConfig(string entryPointPath, string settingsPath)
    {
        if (!string.Equals(Path.GetExtension(entryPointPath), ".dll", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var runtimeConfigPath = Path.ChangeExtension(entryPointPath, ".runtimeconfig.json");
        if (File.Exists(runtimeConfigPath))
        {
            return true;
        }

        return TryWriteSyntheticRuntimeConfig(settingsPath, runtimeConfigPath);
    }

    private static bool TryWriteSyntheticRuntimeConfig(string settingsPath, string runtimeConfigPath)
    {
        try
        {
            if (!DotnetTargetFrameworkRuntimeSupport.TryResolveTargetFrameworkMonikerFromSettingsPath(
                settingsPath,
                out var targetFrameworkMoniker))
            {
                return false;
            }

            var requirement = DotnetTargetFrameworkRuntimeSupport.TryResolveRequirement(targetFrameworkMoniker);
            if (requirement is null)
            {
                return false;
            }

            var runtimeConfig = new JsonObject
            {
                ["runtimeOptions"] = new JsonObject
                {
                    ["tfm"] = targetFrameworkMoniker,
                    ["framework"] = new JsonObject
                    {
                        ["name"] = requirement.Name,
                        ["version"] = requirement.Version,
                    },
                },
            };

            File.WriteAllText(runtimeConfigPath, runtimeConfig.ToJsonString());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryValidateManagedEntryPoint(string entryPointPath, out string failureMessage)
    {
        if (!string.Equals(Path.GetExtension(entryPointPath), ".dll", StringComparison.OrdinalIgnoreCase))
        {
            failureMessage = string.Empty;
            return false;
        }

        try
        {
            using var stream = File.OpenRead(entryPointPath);
            using var peReader = new PEReader(stream);
            if (!peReader.HasMetadata)
            {
                failureMessage = $"Dotnet tool entry point '{Path.GetFileName(entryPointPath)}' is not a managed .NET assembly.";
                return true;
            }

            _ = peReader.GetMetadataReader();
            if (peReader.PEHeaders.CorHeader?.EntryPointTokenOrRelativeVirtualAddress > 0)
            {
                failureMessage = string.Empty;
                return false;
            }

            failureMessage = $"Dotnet tool entry point '{Path.GetFileName(entryPointPath)}' does not contain a managed entry point.";
            return true;
        }
        catch (BadImageFormatException)
        {
            failureMessage = $"Dotnet tool entry point '{Path.GetFileName(entryPointPath)}' is not a valid managed .NET assembly.";
            return true;
        }
        catch (IOException ex)
        {
            failureMessage = $"Dotnet tool entry point '{Path.GetFileName(entryPointPath)}' could not be inspected: {ex.Message}";
            return true;
        }
    }

    private static string ResolveDotnetHostPath()
        => DotnetHostPathResolutionSupport.ResolveDotnetHostPath();
}
