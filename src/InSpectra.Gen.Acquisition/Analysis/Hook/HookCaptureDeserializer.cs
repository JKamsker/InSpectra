namespace InSpectra.Gen.Acquisition.Analysis.Hook;

using System.Diagnostics;
using System.Text.Json;

internal static class HookCaptureDeserializer
{
    public static HookCaptureResult? Deserialize(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<HookCaptureResult>(json);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            Trace.TraceWarning($"Failed to deserialize hook capture from '{path}': {ex.Message}");
            return null;
        }
    }
}


