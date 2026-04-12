using System.Text.Json.Serialization;

namespace InSpectra.Gen.StartupHook.Capture;

internal sealed class CapturedArgument
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isHidden")]
    public bool IsHidden { get; set; }

    [JsonPropertyName("minArity")]
    public int MinArity { get; set; }

    [JsonPropertyName("maxArity")]
    public int MaxArity { get; set; }

    [JsonPropertyName("valueType")]
    public string? ValueType { get; set; }

    [JsonPropertyName("hasDefaultValue")]
    public bool HasDefaultValue { get; set; }

    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; set; }

    [JsonPropertyName("allowedValues")]
    public List<string>? AllowedValues { get; set; }
}
