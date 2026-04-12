using System.Reflection;

namespace InSpectra.Gen.StartupHook.Reflection;

internal static class ReflectionTypeDiscoverySupport
{
    public static IEnumerable<Assembly> GetApplicationAssemblies()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var entryAssemblyDirectory = GetAssemblyDirectory(entryAssembly);
        var yieldedEntryAssembly = false;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            if (entryAssembly is not null && ReferenceEquals(assembly, entryAssembly))
            {
                yieldedEntryAssembly = true;
                yield return assembly;
                continue;
            }

            if (entryAssemblyDirectory is not null
                && !string.Equals(GetAssemblyDirectory(assembly), entryAssemblyDirectory, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return assembly;
        }

        if (!yieldedEntryAssembly && entryAssembly is not null && !entryAssembly.IsDynamic)
        {
            yield return entryAssembly;
        }
    }

    public static IEnumerable<Type> GetLoadableExportedTypes(Assembly assembly)
        => GetLoadableTypes(assembly, static candidate => candidate.GetExportedTypes());

    public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        => GetLoadableTypes(assembly, static candidate => candidate.GetTypes());

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly, Func<Assembly, Type[]> loader)
    {
        try
        {
            return loader(assembly);
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(static type => type is not null)!;
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    private static string? GetAssemblyDirectory(Assembly? assembly)
    {
        if (assembly is null)
        {
            return null;
        }

        try
        {
            return Path.GetDirectoryName(assembly.Location);
        }
        catch
        {
            return null;
        }
    }
}
