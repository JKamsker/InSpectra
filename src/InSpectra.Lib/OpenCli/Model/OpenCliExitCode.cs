using System.Text.Json.Serialization;

namespace InSpectra.Lib.OpenCli.Model;

internal sealed class OpenCliExitCode
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
