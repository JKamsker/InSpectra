using InSpectra.Gen.StartupHook.Reflection;
using System.Collections.Concurrent;
using System.Reflection;

namespace InSpectra.Gen.StartupHook.CommandLineUtils;

internal static class CommandLineUtilsApplicationSupport
{
    public static IEnumerable<object> EnumerateCapturedRootApplications(ConcurrentQueue<object> capturedRootApplications)
    {
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var root in capturedRootApplications)
        {
            if (seen.Add(root))
            {
                yield return root;
            }
        }
    }

    public static object? ResolveRootApplication(
        object instance,
        ConcurrentQueue<object> capturedRootApplications,
        string? cliFramework)
    {
        return CommandLineUtilsPatchingSupport.IsCommandLineApplicationType(instance.GetType(), cliFramework)
            ? NavigateToRoot(instance, cliFramework)
            : EnumerateCapturedRootApplications(capturedRootApplications).FirstOrDefault()
                ?? FindRootApplicationFromLoadedTypes(cliFramework);
    }

    public static object NavigateToRoot(object application, string? cliFramework)
    {
        var current = application;
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        while (visited.Add(current))
        {
            var parent = ReflectionValueReader.GetMemberValue(current, "Parent");
            if (parent is null || !CommandLineUtilsPatchingSupport.IsCommandLineApplicationType(parent.GetType(), cliFramework))
            {
                break;
            }

            current = parent;
        }

        return current;
    }

    public static object? FindRootApplicationFromLoadedTypes(string? cliFramework)
    {
        foreach (var assembly in ReflectionTypeDiscoverySupport.GetApplicationAssemblies())
        {
            foreach (var type in ReflectionTypeDiscoverySupport.GetLoadableTypes(assembly))
            {
                foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    object? value;
                    try
                    {
                        value = field.GetValue(null);
                    }
                    catch
                    {
                        continue;
                    }

                    if (value is not null && CommandLineUtilsPatchingSupport.IsCommandLineApplicationType(value.GetType(), cliFramework))
                    {
                        return NavigateToRoot(value, cliFramework);
                    }
                }
            }
        }

        return null;
    }
}
