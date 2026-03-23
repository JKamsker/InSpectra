using System.Reflection;
using Json.Schema;

namespace OpenCli.Renderer.Services;

public sealed class OpenCliSchemaProvider
{
    private readonly Lazy<JsonSchema> _schema = new(LoadSchema);

    public JsonSchema GetSchema() => _schema.Value;

    private static JsonSchema LoadSchema()
    {
        const string resourceName = "OpenCli.Renderer.Schema.OpenCli.draft.json";

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource `{resourceName}` was not found.");
        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
    }
}
