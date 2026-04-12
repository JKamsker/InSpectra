using System.Text.Json;
using System.Text.Json.Serialization;
using InSpectra.Gen.Engine.OpenCli.Model;

namespace InSpectra.Gen.Engine.OpenCli.Serialization;

internal sealed class OpenCliDocumentSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public string Serialize(OpenCliDocument document)
        => JsonSerializer.Serialize(document, SerializerOptions);
}
