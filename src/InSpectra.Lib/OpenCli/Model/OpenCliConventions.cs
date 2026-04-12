using System.Text.Json.Serialization;

namespace InSpectra.Lib.OpenCli.Model;

internal sealed class OpenCliConventions
{
    [JsonPropertyName("groupOptions")]
    public bool? GroupOptions { get; init; }

    [JsonPropertyName("optionSeparator")]
    public string? OptionSeparator { get; init; }
}
