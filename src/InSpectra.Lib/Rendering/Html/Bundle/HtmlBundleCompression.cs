using System.IO.Compression;

namespace InSpectra.Lib.Rendering.Html.Bundle;

internal static class HtmlBundleCompression
{
    public static string GzipBase64(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(bytes);
        }

        return Convert.ToBase64String(output.ToArray());
    }
}
