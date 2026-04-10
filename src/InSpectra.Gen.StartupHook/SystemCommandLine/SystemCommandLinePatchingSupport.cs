using InSpectra.Gen.StartupHook.Reflection;
using System.Reflection;
using HarmonyLib;

namespace InSpectra.Gen.StartupHook.SystemCommandLine;

internal static class SystemCommandLinePatchingSupport
{
    public static int TryPatchAll(
        Harmony harmony,
        Assembly assembly,
        string methodName,
        HarmonyMethod? prefix = null,
        HarmonyMethod? postfix = null,
        Action<string>? log = null)
    {
        var count = 0;

        foreach (var type in ReflectionTypeDiscoverySupport.GetLoadableExportedTypes(assembly))
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == methodName)
                .Where(m => !m.IsSpecialName)
                .Where(m => IsCommandLineRelated(m, methodName))
                .ToArray();

            foreach (var method in methods)
            {
                try
                {
                    harmony.Patch(method, prefix: prefix, postfix: postfix);
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

    private static bool IsCommandLineRelated(MethodInfo method, string methodName)
    {
        var declaringType = method.DeclaringType;
        if (declaringType is null || typeof(Delegate).IsAssignableFrom(declaringType))
        {
            return false;
        }

        if (method.Name == "Parse" && method.ReturnType == typeof(void))
        {
            return false;
        }

        var typeFullName = declaringType.FullName ?? "";
        return typeFullName.StartsWith("System.CommandLine", StringComparison.Ordinal);
    }
}
