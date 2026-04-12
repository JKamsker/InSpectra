using System.Text.Json.Serialization;

namespace InSpectra.Gen.Engine.OpenCli.Model;

internal sealed class OpenCliContact
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }
}
