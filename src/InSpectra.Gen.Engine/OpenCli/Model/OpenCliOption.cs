using System.Text.Json.Serialization;

namespace InSpectra.Gen.Engine.OpenCli.Model;

internal sealed class OpenCliOption
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("aliases")]
    public List<string> Aliases { get; init; } = [];

    [JsonPropertyName("arguments")]
    public List<OpenCliArgument> Arguments { get; init; } = [];

    [JsonPropertyName("group")]
    public string? Group { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("recursive")]
    public bool Recursive { get; init; }

    [JsonPropertyName("hidden")]
    public bool Hidden { get; init; }

    [JsonPropertyName("metadata")]
    public List<OpenCliMetadata> Metadata { get; init; } = [];
}
