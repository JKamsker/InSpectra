using System.Text.Json;
using System.Text.Json.Serialization;
using InSpectra.Lib.OpenCli.Model;

namespace InSpectra.Lib.OpenCli.Serialization;

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
