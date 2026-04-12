using System.Text.Json.Serialization;

namespace InSpectra.Gen.StartupHook.Capture;

internal sealed class CapturedCommand
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isHidden")]
    public bool IsHidden { get; set; }

    [JsonPropertyName("aliases")]
    public List<string> Aliases { get; set; } = [];

    [JsonPropertyName("options")]
    public List<CapturedOption> Options { get; set; } = [];

    [JsonPropertyName("arguments")]
    public List<CapturedArgument> Arguments { get; set; } = [];

    [JsonPropertyName("subcommands")]
    public List<CapturedCommand> Subcommands { get; set; } = [];
}
