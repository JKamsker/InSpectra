using InSpectra.Gen.StartupHook.Capture;
using InSpectra.Gen.StartupHook.Frameworks;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace InSpectra.Gen.StartupHook.SystemCommandLine;

internal static class HarmonyPatchInstaller
{
    internal static Assembly? SystemCommandLineAssembly;
    internal static string? CapturePath;
    private static int _captured; // 0 = idle, 1 = capturing, 2 = captured
    private static readonly ConcurrentBag<string> _patchLog = [];
    private static readonly ConcurrentQueue<object> _capturedRootCommands = [];
    private static string? _noPatchableMethodDiagnostic;

    public static string GetPatchLog() => string.Join("\n", _patchLog);

    public static void Install(Assembly sclAssembly, string capturePath)
    {
        SystemCommandLineAssembly = sclAssembly;
        CapturePath = capturePath;
        while (_patchLog.TryTake(out _)) { }
        while (_capturedRootCommands.TryDequeue(out _)) { }
        HookCaptureStateSupport.Reset(ref _captured);
        _noPatchableMethodDiagnostic = null;

        var harmony = new Harmony("com.inspectra.discovery.startuphook");

        var postfix = new HarmonyMethod(typeof(HarmonyPatchInstaller), nameof(ParsePostfix));
        var invokePostfix = new HarmonyMethod(typeof(HarmonyPatchInstaller), nameof(InvokePostfix));
        var patchCount = 0;

        // Parse methods capture the ParseResult return value when the public API exposes it.
        patchCount += SystemCommandLinePatchingSupport.TryPatchAll(harmony, sclAssembly, "Parse", postfix: postfix, log: _patchLog.Add);

        // Invoke methods are observed after they return so the hook never changes target control flow.
        patchCount += SystemCommandLinePatchingSupport.TryPatchAll(harmony, sclAssembly, "Invoke", postfix: invokePostfix, log: _patchLog.Add);
        patchCount += SystemCommandLinePatchingSupport.TryPatchAll(harmony, sclAssembly, "InvokeAsync", postfix: invokePostfix, log: _patchLog.Add);

        // Fallback: patch RootCommand constructors to capture the instance.
        // When Harmony patches on public API methods don't fire (e.g., R2R precompiled tools
        // or tools that use internal API paths), we capture the RootCommand via its constructor
        // and serialize it on ProcessExit.
        var rootCommandType = sclAssembly.GetType("System.CommandLine.RootCommand");
        if (rootCommandType is not null)
        {
            var ctorPostfix = new HarmonyMethod(typeof(HarmonyPatchInstaller), nameof(RootCommandCtorPostfix));
            foreach (var ctor in rootCommandType.GetConstructors())
            {
                try
                {
                    harmony.Patch(ctor, postfix: ctorPostfix);
                    _patchLog.Add($"OK: RootCommand.ctor({string.Join(", ", ctor.GetParameters().Select(p => p.ParameterType.Name))})");
                    patchCount++;
                }
                catch (Exception ex)
                {
                    _patchLog.Add($"FAIL: RootCommand.ctor: {ex.Message}");
                }
            }
        }

        // Register ProcessExit to capture from a stored RootCommand if earlier patches never resolved one.
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        if (patchCount == 0)
        {
            var diag = new System.Text.StringBuilder();
            diag.AppendLine($"Assembly: {sclAssembly.FullName}");
            diag.AppendLine($"Patch log: {string.Join("; ", _patchLog)}");
            _noPatchableMethodDiagnostic = diag.ToString();
        }
    }

    /// <summary>
    /// Postfix on RootCommand constructor — captures the instance when created.
    /// </summary>
    public static void RootCommandCtorPostfix(object __instance)
    {
        _capturedRootCommands.Enqueue(__instance);
    }

    /// <summary>
    /// ProcessExit handler — if no earlier patch fired, serialize from captured RootCommand.
    /// </summary>
    private static void OnProcessExit(object? sender, EventArgs e)
    {
        if (HookCaptureStateSupport.IsBusyOrCompleted(ref _captured) || CapturePath is null || SystemCommandLineAssembly is null)
        {
            return;
        }

        foreach (var root in EnumerateCapturedRootCommands())
        {
            if (TryCaptureFromObject(root, "ProcessExit-fallback"))
            {
                return;
            }
        }

        var reflectedRoot = SystemCommandLineRootResolutionSupport.FindRootCommandFromLoadedTypes(SystemCommandLineAssembly);
        if (reflectedRoot is not null && TryCaptureFromObject(reflectedRoot, "ProcessExit-fallback"))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_noPatchableMethodDiagnostic))
        {
            CaptureFileWriter.WriteError(CapturePath, "no-patchable-method", _noPatchableMethodDiagnostic, overwrite: false);
        }
    }

    /// <summary>
    /// Postfix on Parse methods — fires AFTER Parse returns.
    /// The __result is the ParseResult, from which we extract the Command tree.
    /// </summary>
    public static void ParsePostfix(object? __instance, object? __result)
    {
        if (HookCaptureStateSupport.IsBusyOrCompleted(ref _captured) || __result is null) return;
        TryCaptureFromObject(__result, "Parse-postfix");
    }

    /// <summary>
    /// Postfix on Invoke/InvokeAsync — observes the command surface without short-circuiting the target.
    /// </summary>
    public static void InvokePostfix(object? __instance, object[]? __args)
    {
        if (HookCaptureStateSupport.IsBusyOrCompleted(ref _captured))
        {
            return;
        }

        if (__instance is not null && TryCaptureFromObject(__instance, "Invoke-postfix"))
        {
            return;
        }

        if (__args is null)
        {
            return;
        }

        foreach (var argument in __args)
        {
            if (argument is not null && TryCaptureFromObject(argument, "Invoke-postfix"))
            {
                return;
            }
        }
    }

    private static bool TryCaptureFromObject(object? target, string source)
    {
        if (target is null || SystemCommandLineAssembly is null || CapturePath is null)
            return false;

        var rootCommand = SystemCommandLineRootResolutionSupport.ResolveRootCommand(target)
            ?? SystemCommandLineRootResolutionSupport.FindRootCommandFromLoadedTypes(SystemCommandLineAssembly);
        if (rootCommand is null)
            return false;

        if (!HookCaptureStateSupport.TryBegin(ref _captured))
        {
            return HookCaptureStateSupport.IsBusyOrCompleted(ref _captured);
        }

        var completed = false;

        try
        {
            if (HookCaptureStateSupport.HasPreservedFailure(CapturePath))
            {
                HookCaptureStateSupport.Complete(ref _captured);
                completed = true;
                return true;
            }

            var tree = CommandTreeWalker.Walk(rootCommand, SystemCommandLineAssembly);
            var version = SystemCommandLineAssembly.GetName().Version?.ToString();
            if (!CaptureFileWriter.Write(CapturePath, new CaptureResult
            {
                Status = "ok",
                CliFramework = HookCliFrameworkSupport.SystemCommandLine,
                FrameworkVersion = version,
                SystemCommandLineVersion = version,
                PatchTarget = $"{source} ({string.Join(", ", _patchLog.Where(l => l.StartsWith("OK")))})",
                Root = tree,
            }))
            {
                return false;
            }

            HookCaptureStateSupport.Complete(ref _captured);
            completed = true;
            return true;
        }
        catch (Exception ex)
        {
            CaptureFileWriter.WriteError(CapturePath, "capture-failed", ex.ToString());
            return false;
        }
        finally
        {
            if (!completed)
            {
                HookCaptureStateSupport.Release(ref _captured);
            }
        }
    }

    private static IEnumerable<object> EnumerateCapturedRootCommands()
        => SystemCommandLineRootResolutionSupport.EnumerateCapturedRootCommands(_capturedRootCommands);
}
