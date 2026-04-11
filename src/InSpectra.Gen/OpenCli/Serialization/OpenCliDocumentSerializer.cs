using System.Text.Json;
using InSpectra.Gen.Output.Json;

namespace InSpectra.Gen.OpenCli.Serialization;

public sealed class OpenCliDocumentSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonOutput.SerializerOptions.DefaultIgnoreCondition,
        WriteIndented = true,
    };

    public string Serialize(OpenCliDocument document)
        => JsonSerializer.Serialize(document, SerializerOptions);
}
