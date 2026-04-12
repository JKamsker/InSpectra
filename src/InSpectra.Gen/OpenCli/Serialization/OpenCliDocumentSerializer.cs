using System.Text.Json;
using System.Text.Json.Serialization;
using InSpectra.Gen.OpenCli.Model;

namespace InSpectra.Gen.OpenCli.Serialization;

public sealed class OpenCliDocumentSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public string Serialize(OpenCliDocument document)
        => JsonSerializer.Serialize(document, SerializerOptions);
}
