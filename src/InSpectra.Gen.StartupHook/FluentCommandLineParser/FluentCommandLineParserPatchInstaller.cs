using InSpectra.Gen.StartupHook.Capture;
using InSpectra.Gen.StartupHook.Frameworks;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace InSpectra.Gen.StartupHook.FluentCommandLineParser;

internal static class FluentCommandLineParserPatchInstaller
{
    internal static Assembly? FrameworkAssembly;
    internal static string? CapturePath;

    private static readonly ConcurrentBag<string> PatchLog = [];
    private static int _captured; // 0 = idle, 1 = capturing, 2 = captured
    private static string? _noPatchableMethodDiagnostic;

    public static void Install(Assembly assembly, string capturePath)
    {
        FrameworkAssembly = assembly;
        CapturePath = capturePath;
        while (PatchLog.TryTake(out _)) { }
        HookCaptureStateSupport.Reset(ref _captured);
        _noPatchableMethodDiagnostic = null;

        var harmony = new Harmony("com.inspectra.discovery.startuphook.fluentcommandlineparser");
        var parsePostfix = new HarmonyMethod(typeof(FluentCommandLineParserPatchInstaller), nameof(ParsePostfix));

        var patchCount = 0;
        patchCount += TryPatchParseMethod(harmony, assembly.GetType("Fclp.FluentCommandLineParser"), parsePostfix);
        patchCount += TryPatchParseMethod(harmony, assembly.GetType("Fclp.FluentCommandLineParser`1"), parsePostfix);

        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        if (patchCount == 0)
        {
            var diagnostic = new System.Text.StringBuilder();
            diagnostic.AppendLine($"Assembly: {assembly.FullName}");
            diagnostic.AppendLine($"Patch log: {string.Join("; ", PatchLog)}");
            _noPatchableMethodDiagnostic = diagnostic.ToString();
        }
    }

    public static void ParsePostfix(MethodBase? __originalMethod, object? __instance)
    {
        if (HookCaptureStateSupport.IsBusyOrCompleted(ref _captured) || __instance is null || FrameworkAssembly is null || CapturePath is null)
        {
            return;
        }

        if (!FluentCommandLineParserTreeWalker.TryWalk(__instance, out var root))
        {
            return;
        }

        if (!HookCaptureStateSupport.TryBegin(ref _captured))
        {
            return;
        }

        var completed = false;

        try
        {
            if (HookCaptureStateSupport.HasPreservedFailure(CapturePath))
            {
                HookCaptureStateSupport.Complete(ref _captured);
                completed = true;
                return;
            }

            if (!CaptureFileWriter.Write(CapturePath, new CaptureResult
            {
                Status = "ok",
                CliFramework = HookCliFrameworkSupport.FluentCommandLineParser,
                FrameworkVersion = FrameworkAssembly.GetName().Version?.ToString(),
                PatchTarget = FormatPatchTarget(__originalMethod),
                Root = root,
            }))
            {
                return;
            }

            HookCaptureStateSupport.Complete(ref _captured);
            completed = true;
        }
        catch (Exception ex)
        {
            CaptureFileWriter.WriteError(CapturePath, "capture-failed", ex.ToString());
        }
        finally
        {
            if (!completed)
            {
                HookCaptureStateSupport.Release(ref _captured);
            }
        }
    }

    private static int TryPatchParseMethod(Harmony harmony, Type? type, HarmonyMethod postfix)
    {
        if (type is null)
        {
            return 0;
        }

        var count = 0;
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (!string.Equals(method.Name, "Parse", StringComparison.Ordinal) || method.IsSpecialName)
            {
                continue;
            }

            try
            {
                harmony.Patch(method, postfix: postfix);
                PatchLog.Add($"OK: {type.Name}.{method.Name}");
                count++;
            }
            catch (Exception ex)
            {
                PatchLog.Add($"FAIL: {type.Name}.{method.Name}: {ex.Message}");
            }
        }

        return count;
    }

    private static string FormatPatchTarget(MethodBase? method)
    {
        if (method is null)
        {
            return string.Join(", ", PatchLog.Where(entry => entry.StartsWith("OK", StringComparison.Ordinal)));
        }

        var methodName = $"{method.DeclaringType?.Name}.{method.Name}";
        var patchSummary = string.Join(", ", PatchLog.Where(entry => entry.StartsWith("OK", StringComparison.Ordinal)));
        return string.IsNullOrWhiteSpace(patchSummary)
            ? methodName
            : $"{methodName} ({patchSummary})";
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        if (HookCaptureStateSupport.IsBusyOrCompleted(ref _captured)
            || string.IsNullOrWhiteSpace(_noPatchableMethodDiagnostic)
            || CapturePath is null)
        {
            return;
        }

        CaptureFileWriter.WriteError(CapturePath, "no-patchable-method", _noPatchableMethodDiagnostic, overwrite: false);
    }
}
