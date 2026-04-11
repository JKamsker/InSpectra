namespace InSpectra.Gen.Acquisition.Modes.Hook.Capture;

using System.Diagnostics;
using System.Text.Json;

internal static class HookCaptureDeserializer
{
    private const int ExpectedCaptureVersion = 1;

    public static HookCaptureResult? Deserialize(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var capture = JsonSerializer.Deserialize<HookCaptureResult>(json);
            if (capture is null)
            {
                return null;
            }

            if (capture.CaptureVersion == ExpectedCaptureVersion)
            {
                return capture;
            }

            return new HookCaptureResult
            {
                CaptureVersion = capture.CaptureVersion,
                Status = "capture-version-mismatch",
                Error = $"Hook capture version `{capture.CaptureVersion}` is not supported. Expected `{ExpectedCaptureVersion}`.",
            };
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            Trace.TraceWarning($"Failed to deserialize hook capture from '{path}': {ex.Message}");
            return null;
        }
    }
}


