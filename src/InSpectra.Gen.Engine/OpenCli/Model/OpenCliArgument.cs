using System.Text.Json.Serialization;

namespace InSpectra.Gen.Engine.OpenCli.Model;

internal sealed class OpenCliArgument
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("arity")]
    public OpenCliArity? Arity { get; init; }

    [JsonPropertyName("acceptedValues")]
    public List<string> AcceptedValues { get; init; } = [];

    [JsonPropertyName("group")]
    public string? Group { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("hidden")]
    public bool Hidden { get; init; }

    [JsonPropertyName("metadata")]
    public List<OpenCliMetadata> Metadata { get; init; } = [];
}
