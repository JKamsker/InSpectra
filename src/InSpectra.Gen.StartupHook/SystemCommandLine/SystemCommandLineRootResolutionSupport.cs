using InSpectra.Gen.StartupHook.Reflection;
using System.Collections.Concurrent;
using System.Reflection;

namespace InSpectra.Gen.StartupHook.SystemCommandLine;

internal static class SystemCommandLineRootResolutionSupport
{
    public static IEnumerable<object> EnumerateCapturedRootCommands(ConcurrentQueue<object> capturedRootCommands)
    {
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var root in capturedRootCommands)
        {
            if (seen.Add(root))
            {
                yield return root;
            }
        }
    }

    public static object? ResolveRootCommand(object instance)
    {
        if (IsCommandType(instance.GetType()))
        {
            return NavigateToRoot(instance);
        }

        var rootCommand = TryNavigateProperty(instance, "RootCommandResult", "Command")
            ?? TryNavigateProperty(instance, "CommandResult", "Command");
        if (rootCommand is not null)
        {
            return NavigateToRoot(rootCommand);
        }

        rootCommand = GetPropertyValue(instance, "RootCommand");
        if (rootCommand is not null && IsCommandType(rootCommand.GetType()))
        {
            return rootCommand;
        }

        var config = GetPropertyValue(instance, "Configuration");
        if (config is not null)
        {
            rootCommand = GetPropertyValue(config, "RootCommand");
            if (rootCommand is not null && IsCommandType(rootCommand.GetType()))
            {
                return rootCommand;
            }
        }

        return null;
    }

    public static object? FindRootCommandFromLoadedTypes(Assembly? systemCommandLineAssembly)
    {
        if (systemCommandLineAssembly is null)
        {
            return null;
        }

        var rootCommandType = systemCommandLineAssembly.GetType("System.CommandLine.RootCommand");
        var commandType = systemCommandLineAssembly.GetType("System.CommandLine.Command");
        if (rootCommandType is null && commandType is null)
        {
            return null;
        }

        foreach (var assembly in ReflectionTypeDiscoverySupport.GetApplicationAssemblies())
        {
            foreach (var type in ReflectionTypeDiscoverySupport.GetLoadableTypes(assembly))
            {
                foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    try
                    {
                        if ((rootCommandType?.IsAssignableFrom(field.FieldType) ?? false)
                            || (commandType?.IsAssignableFrom(field.FieldType) ?? false))
                        {
                            var value = field.GetValue(null);
                            if (value is not null)
                            {
                                return value;
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        return null;
    }

    public static bool IsCommandType(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.FullName is "System.CommandLine.Command" or "System.CommandLine.RootCommand")
            {
                return true;
            }
        }

        return false;
    }

    private static object? TryNavigateProperty(object instance, string first, string second)
    {
        try
        {
            var intermediate = GetPropertyValue(instance, first);
            if (intermediate is null)
            {
                return null;
            }

            var result = GetPropertyValue(intermediate, second);
            return result is not null && IsCommandType(result.GetType()) ? result : null;
        }
        catch
        {
            return null;
        }
    }

    private static object? GetPropertyValue(object obj, string name)
    {
        try
        {
            return obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }

    private static object NavigateToRoot(object command)
    {
        var current = command;
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        while (visited.Add(current))
        {
            var parent = GetPropertyValue(current, "Parent");
            if (parent is null || !IsCommandType(parent.GetType()))
            {
                break;
            }

            current = parent;
        }

        return current;
    }
}
