using System.Text.Json.Serialization;

namespace InSpectra.Lib.OpenCli.Model;

internal sealed class OpenCliCommand
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("aliases")]
    public List<string> Aliases { get; init; } = [];

    [JsonPropertyName("options")]
    public List<OpenCliOption> Options { get; init; } = [];

    [JsonPropertyName("arguments")]
    public List<OpenCliArgument> Arguments { get; init; } = [];

    [JsonPropertyName("commands")]
    public List<OpenCliCommand> Commands { get; init; } = [];

    [JsonPropertyName("exitCodes")]
    public List<OpenCliExitCode> ExitCodes { get; init; } = [];

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("hidden")]
    public bool Hidden { get; init; }

    [JsonPropertyName("examples")]
    public List<string> Examples { get; init; } = [];

    [JsonPropertyName("interactive")]
    public bool Interactive { get; init; }

    [JsonPropertyName("metadata")]
    public List<OpenCliMetadata> Metadata { get; init; } = [];
}
