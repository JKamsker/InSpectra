using System.Text.Json.Serialization;

namespace InSpectra.Gen.Cli.Output.Json;

public sealed class JsonError
{
    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("details")]
    public IReadOnlyList<string> Details { get; init; } = [];
}
