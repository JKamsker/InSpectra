using System.Text.Json.Serialization;

namespace InSpectra.Gen.Cli.Output.Json;

public sealed class JsonEnvelope<T>
{
    [JsonPropertyName("ok")]
    public required bool Ok { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("error")]
    public JsonError? Error { get; init; }

    [JsonPropertyName("meta")]
    public required JsonMeta Meta { get; init; }
}
