using System.Text.Json.Serialization;

namespace InSpectra.Gen.Engine.OpenCli.Model;

internal sealed class OpenCliLicense
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}
