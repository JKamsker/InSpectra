using System.Collections.Concurrent;
using System.Text.Json;

namespace InSpectra.Gen.StartupHook.Capture;

internal static class CaptureFileWriter
{
    private static readonly ConcurrentDictionary<string, object> PathLocks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        // TODO: Replace with DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        // when the TFM is upgraded to net5.0+. DefaultIgnoreCondition was introduced in .NET 5;
        // IgnoreNullValues is the only option available on netcoreapp3.1.
        IgnoreNullValues = true,
    };

    public static bool Write(string path, CaptureResult result, bool overwrite = true)
    {
        var normalizedPath = Path.GetFullPath(path);
        lock (GetPathLock(normalizedPath))
        {
            if (overwrite && HookCaptureStateSupport.IsPreservedFailureStatus(TryReadStatusCore(normalizedPath)))
            {
                return false;
            }

            return WriteCore(normalizedPath, result, overwrite);
        }
    }

    public static bool WritePreservedError(string path, string status, string error)
    {
        var normalizedPath = Path.GetFullPath(path);
        lock (GetPathLock(normalizedPath))
        {
            return WriteCore(normalizedPath, new CaptureResult
            {
                Status = status,
                Error = error,
            }, overwrite: true);
        }
    }

    public static bool WriteError(string path, string status, string error, bool overwrite = true)
        => Write(path, new CaptureResult
        {
            Status = status,
            Error = error,
        }, overwrite);

    public static string? TryReadStatus(string path)
    {
        var normalizedPath = Path.GetFullPath(path);
        lock (GetPathLock(normalizedPath))
        {
            return TryReadStatusCore(normalizedPath);
        }
    }

    private static bool WriteCore(string path, CaptureResult result, bool overwrite)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(result, JsonOptions);
            using var stream = new FileStream(
                path,
                overwrite ? FileMode.Create : FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read);
            using var writer = new StreamWriter(stream);
            writer.Write(json);
            return true;
        }
        catch (Exception ex)
        {
            // Best-effort: if we can't write, the main tool will see a missing file.
            Console.Error.WriteLine($"[InSpectra] Failed to write capture file: {ex.Message}");
            return false;
        }
    }

    private static string? TryReadStatusCore(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            return document.RootElement.TryGetProperty("status", out var status)
                ? status.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static object GetPathLock(string path)
        => PathLocks.GetOrAdd(path, static _ => new object());
}
