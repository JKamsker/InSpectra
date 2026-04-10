using InSpectra.Gen.StartupHook.Capture;
using InSpectra.Gen.StartupHook.Frameworks;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace InSpectra.Gen.StartupHook.CommandLineUtils;

internal static class CommandLineUtilsPatchInstaller
{
    internal static Assembly? FrameworkAssembly;
    internal static string? CliFramework;
    internal static string? CapturePath;

    private static readonly ConcurrentBag<string> PatchLog = [];
    private static readonly ConcurrentQueue<object> CapturedRootApplications = [];
    private static int _captured; // 0 = idle, 1 = capturing, 2 = captured
    private static string? _noPatchableMethodDiagnostic;

    public static void Install(Assembly assembly, string cliFramework, string capturePath)
    {
        FrameworkAssembly = assembly;
        CliFramework = cliFramework;
        CapturePath = capturePath;
        while (PatchLog.TryTake(out _)) { }
        while (CapturedRootApplications.TryDequeue(out _)) { }
        HookCaptureStateSupport.Reset(ref _captured);
        _noPatchableMethodDiagnostic = null;

        var harmony = new Harmony("com.inspectra.discovery.startuphook.commandlineutils");
        var parsePostfix = new HarmonyMethod(typeof(CommandLineUtilsPatchInstaller), nameof(ParsePostfix));
        var executePostfix = new HarmonyMethod(typeof(CommandLineUtilsPatchInstaller), nameof(ExecutePostfix));
        var executeFinalizer = new HarmonyMethod(typeof(CommandLineUtilsPatchInstaller), nameof(ExecuteFinalizer));
        var constructorPostfix = new HarmonyMethod(typeof(CommandLineUtilsPatchInstaller), nameof(CommandLineApplicationConstructorPostfix));

        var patchCount = 0;
        patchCount += CommandLineUtilsPatchingSupport.TryPatchNamedMethods(harmony, assembly, "Parse", parsePostfix, cliFramework, PatchLog.Add);
        patchCount += CommandLineUtilsPatchingSupport.TryPatchNamedMethods(harmony, assembly, "Execute", executePostfix, cliFramework, PatchLog.Add, executeFinalizer);
        patchCount += CommandLineUtilsPatchingSupport.TryPatchNamedMethods(harmony, assembly, "ExecuteAsync", executePostfix, cliFramework, PatchLog.Add, executeFinalizer);
        patchCount += CommandLineUtilsPatchingSupport.TryPatchConstructors(harmony, assembly, constructorPostfix, cliFramework, PatchLog.Add);

        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        if (patchCount == 0)
        {
            var diagnostic = new System.Text.StringBuilder();
            diagnostic.AppendLine($"Framework: {cliFramework}");
            diagnostic.AppendLine($"Assembly: {assembly.FullName}");
            diagnostic.AppendLine($"Patch log: {string.Join("; ", PatchLog)}");
            _noPatchableMethodDiagnostic = diagnostic.ToString();
        }
    }

    public static void CommandLineApplicationConstructorPostfix(object __instance)
    {
        if (HookCaptureStateSupport.IsBusyOrCompleted(ref _captured) || __instance is null)
        {
            return;
        }

        CapturedRootApplications.Enqueue(CommandLineUtilsApplicationSupport.NavigateToRoot(__instance, CliFramework));
    }

    public static void ParsePostfix(object? __instance)
    {
        if (HookCaptureStateSupport.IsBusyOrCompleted(ref _captured) || __instance is null)
        {
            return;
        }

        TryCaptureFromObject(__instance, "Parse-postfix");
    }

    public static void ExecutePostfix(object? __instance)
    {
        if (HookCaptureStateSupport.IsBusyOrCompleted(ref _captured))
        {
            return;
        }

        if (__instance is not null && TryCaptureFromObject(__instance, "Execute-postfix"))
        {
            return;
        }

        TryCaptureFromCapturedRoots("Execute-postfix");
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        if (HookCaptureStateSupport.IsBusyOrCompleted(ref _captured))
        {
            return;
        }

        foreach (var rootApplication in CommandLineUtilsApplicationSupport.EnumerateCapturedRootApplications(CapturedRootApplications))
        {
            if (TryCaptureFromObject(rootApplication, "ProcessExit-fallback"))
            {
                return;
            }
        }

        var reflectedRoot = CommandLineUtilsApplicationSupport.FindRootApplicationFromLoadedTypes(CliFramework);
        if (reflectedRoot is not null && TryCaptureFromObject(reflectedRoot, "ProcessExit-fallback"))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_noPatchableMethodDiagnostic) && CapturePath is not null)
        {
            CaptureFileWriter.WriteError(CapturePath, "no-patchable-method", _noPatchableMethodDiagnostic, overwrite: false);
        }
    }

    public static Exception? ExecuteFinalizer(object? __instance, Exception? __exception)
    {
        if (HookCaptureStateSupport.IsBusyOrCompleted(ref _captured))
        {
            return __exception;
        }

        if (__instance is not null && TryCaptureFromObject(__instance, "Execute-finalizer"))
        {
            return __exception;
        }

        TryCaptureFromCapturedRoots("Execute-finalizer");

        return __exception;
    }

    private static bool TryCaptureFromObject(object target, string source)
    {
        if (FrameworkAssembly is null || CapturePath is null || string.IsNullOrWhiteSpace(CliFramework))
        {
            return false;
        }

        var rootApplication = CommandLineUtilsApplicationSupport.ResolveRootApplication(target, CapturedRootApplications, CliFramework);
        if (rootApplication is null)
        {
            return false;
        }

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

            var version = FrameworkAssembly.GetName().Version?.ToString();
            if (!CaptureFileWriter.Write(CapturePath, new CaptureResult
            {
                Status = "ok",
                CliFramework = CliFramework,
                FrameworkVersion = version,
                SystemCommandLineVersion = string.Equals(CliFramework, HookCliFrameworkSupport.SystemCommandLine, StringComparison.Ordinal)
                    ? version
                    : null,
                PatchTarget = $"{source} ({string.Join(", ", PatchLog.Where(entry => entry.StartsWith("OK", StringComparison.Ordinal)))})",
                Root = CommandLineUtilsTreeWalker.Walk(rootApplication),
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

    private static void TryCaptureFromCapturedRoots(string source)
    {
        foreach (var rootApplication in CommandLineUtilsApplicationSupport.EnumerateCapturedRootApplications(CapturedRootApplications))
        {
            if (TryCaptureFromObject(rootApplication, source))
            {
                return;
            }
        }
    }
}
