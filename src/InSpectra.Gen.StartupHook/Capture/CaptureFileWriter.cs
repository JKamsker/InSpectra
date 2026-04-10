using System.Text.Json;

namespace InSpectra.Gen.StartupHook.Capture;

internal static class CaptureFileWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        // TODO: Replace with DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        // when the TFM is upgraded to net5.0+. DefaultIgnoreCondition was introduced in .NET 5;
        // IgnoreNullValues is the only option available on netcoreapp3.1.
        IgnoreNullValues = true,
    };

    public static void Write(string path, CaptureResult result, bool overwrite = true)
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
        }
        catch (Exception ex)
        {
            // Best-effort: if we can't write, the main tool will see a missing file.
            Console.Error.WriteLine($"[InSpectra] Failed to write capture file: {ex.Message}");
        }
    }

    public static void WriteError(string path, string status, string error, bool overwrite = true)
    {
        Write(path, new CaptureResult
        {
            Status = status,
            Error = error,
        }, overwrite);
    }
}
