using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InSpectra.Lib.OpenCli.Validation;

/// <summary>
/// Cleans up OpenCLI JSON emitted from a subprocess before it is parsed.
/// Spectre.Console wraps long strings at 80 columns even when stdout is
/// redirected, which embeds literal newlines inside JSON string values.
/// This helper collapses any newlines that appear inside JSON strings and
/// then round-trips the result through <see cref="JsonDocument"/> for a
/// clean output. Valid JSON passes through unchanged (aside from
/// reformatting).
/// </summary>
internal static class OpenCliJsonSanitizer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static string Sanitize(string raw)
    {
        var sb = new StringBuilder(raw.Length);
        var inString = false;
        var escaped = false;

        foreach (var ch in raw)
        {
            if (escaped)
            {
                sb.Append(ch);
                escaped = false;
                continue;
            }

            if (ch == '\\' && inString)
            {
                sb.Append(ch);
                escaped = true;
                continue;
            }

            if (ch == '"')
            {
                inString = !inString;
                sb.Append(ch);
                continue;
            }

            if (inString && (ch == '\n' || ch == '\r'))
            {
                if (ch == '\n')
                {
                    sb.Append(' ');
                }

                continue;
            }

            sb.Append(ch);
        }

        using var document = JsonDocument.Parse(sb.ToString());
        return JsonSerializer.Serialize(document, SerializerOptions);
    }
}
