using System.Threading;

namespace InSpectra.Gen.StartupHook.Capture;

internal static class HookCaptureStateSupport
{
    private const int NotCaptured = 0;
    private const int Capturing = 1;
    private const int Captured = 2;

    public const string TargetUnhandledExceptionStatus = "target-unhandled-exception";

    public static void Reset(ref int state)
        => Volatile.Write(ref state, NotCaptured);

    public static bool TryBegin(ref int state)
        => Interlocked.CompareExchange(ref state, Capturing, NotCaptured) == NotCaptured;

    public static void Complete(ref int state)
        => Volatile.Write(ref state, Captured);

    public static void Release(ref int state)
        => Interlocked.CompareExchange(ref state, NotCaptured, Capturing);

    public static bool IsBusyOrCompleted(ref int state)
        => Volatile.Read(ref state) != NotCaptured;

    public static bool HasPreservedFailure(string path)
        => IsPreservedFailureStatus(CaptureFileWriter.TryReadStatus(path));

    public static bool IsPreservedFailureStatus(string? status)
        => string.Equals(status, TargetUnhandledExceptionStatus, StringComparison.Ordinal);
}
