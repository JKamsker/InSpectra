using System.Text.Json.Serialization;

namespace InSpectra.Gen.Engine.OpenCli.Model;

internal sealed class OpenCliConventions
{
    [JsonPropertyName("groupOptions")]
    public bool? GroupOptions { get; init; }

    [JsonPropertyName("optionSeparator")]
    public string? OptionSeparator { get; init; }
}
