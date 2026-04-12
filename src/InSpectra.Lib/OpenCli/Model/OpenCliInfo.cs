using System.Text.Json.Serialization;

namespace InSpectra.Lib.OpenCli.Model;

internal sealed class OpenCliInfo
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("contact")]
    public OpenCliContact? Contact { get; init; }

    [JsonPropertyName("license")]
    public OpenCliLicense? License { get; init; }

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;
}
