using InSpectra.Gen.StartupHook.Reflection;
using System.Reflection;
using HarmonyLib;

namespace InSpectra.Gen.StartupHook.CommandLineUtils;

internal static class CommandLineUtilsPatchingSupport
{
    public static int TryPatchNamedMethods(
        Harmony harmony,
        Assembly assembly,
        string methodName,
        HarmonyMethod postfix,
        string? cliFramework,
        Action<string>? log,
        HarmonyMethod? finalizer = null)
    {
        var count = 0;
        foreach (var type in ReflectionTypeDiscoverySupport.GetLoadableExportedTypes(assembly).Where(type => IsCommandLineApplicationType(type, cliFramework)))
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal)
                    || method.IsSpecialName)
                {
                    continue;
                }

                try
                {
                    harmony.Patch(method, postfix: postfix, finalizer: finalizer);
                    log?.Invoke($"OK: {type.Name}.{method.Name}");
                    count++;
                }
                catch (Exception ex)
                {
                    log?.Invoke($"FAIL: {type.Name}.{method.Name}: {ex.Message}");
                }
            }
        }

        return count;
    }

    public static int TryPatchConstructors(
        Harmony harmony,
        Assembly assembly,
        HarmonyMethod postfix,
        string? cliFramework,
        Action<string>? log)
    {
        var count = 0;
        foreach (var type in ReflectionTypeDiscoverySupport.GetLoadableExportedTypes(assembly).Where(type => IsCommandLineApplicationType(type, cliFramework)))
        {
            foreach (var constructor in type.GetConstructors())
            {
                try
                {
                    harmony.Patch(constructor, postfix: postfix);
                    log?.Invoke($"OK: {type.Name}.ctor({string.Join(", ", constructor.GetParameters().Select(parameter => parameter.ParameterType.Name))})");
                    count++;
                }
                catch (Exception ex)
                {
                    log?.Invoke($"FAIL: {type.Name}.ctor: {ex.Message}");
                }
            }
        }

        return count;
    }

    public static bool IsCommandLineApplicationType(Type type, string? cliFramework)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var fullName = current.FullName;
            if (fullName is null || string.IsNullOrWhiteSpace(cliFramework))
            {
                continue;
            }

            if (fullName.StartsWith(cliFramework + ".CommandLineApplication", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
