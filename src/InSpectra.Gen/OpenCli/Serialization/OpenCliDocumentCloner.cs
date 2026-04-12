using System.Text.Json;
using InSpectra.Gen.OpenCli.Model;

namespace InSpectra.Gen.OpenCli.Serialization;

public sealed class OpenCliDocumentCloner
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public OpenCliDocument Clone(OpenCliDocument document)
    {
        var json = JsonSerializer.Serialize(document, SerializerOptions);
        return JsonSerializer.Deserialize<OpenCliDocument>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Failed to clone the OpenCLI document.");
    }
}
