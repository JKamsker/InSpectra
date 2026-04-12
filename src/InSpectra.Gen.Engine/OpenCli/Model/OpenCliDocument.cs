using System.Text.Json.Serialization;

namespace InSpectra.Gen.Engine.OpenCli.Model;

internal sealed class OpenCliDocument
{
    [JsonPropertyName("opencli")]
    public string OpenCliVersion { get; init; } = string.Empty;

    [JsonPropertyName("info")]
    public OpenCliInfo Info { get; init; } = new();

    [JsonPropertyName("conventions")]
    public OpenCliConventions? Conventions { get; init; }

    [JsonPropertyName("arguments")]
    public List<OpenCliArgument> Arguments { get; init; } = [];

    [JsonPropertyName("options")]
    public List<OpenCliOption> Options { get; init; } = [];

    [JsonPropertyName("commands")]
    public List<OpenCliCommand> Commands { get; init; } = [];

    [JsonPropertyName("exitCodes")]
    public List<OpenCliExitCode> ExitCodes { get; init; } = [];

    [JsonPropertyName("examples")]
    public List<string> Examples { get; init; } = [];

    [JsonPropertyName("interactive")]
    public bool Interactive { get; init; }

    [JsonPropertyName("metadata")]
    public List<OpenCliMetadata> Metadata { get; init; } = [];
}
