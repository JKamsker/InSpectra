using System.Text.Json.Serialization;

namespace InSpectra.Lib.OpenCli.Model;

internal sealed class OpenCliArity
{
    [JsonPropertyName("minimum")]
    public int? Minimum { get; init; }

    [JsonPropertyName("maximum")]
    public int? Maximum { get; init; }
}
