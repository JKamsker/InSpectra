using System.Reflection;
using Json.Schema;

namespace InSpectra.Gen.Engine.OpenCli.Schema;

internal sealed class OpenCliSchemaProvider
{
    private static readonly Lazy<JsonSchema> Schema = new(LoadSchema);

    public JsonSchema GetSchema() => Schema.Value;

    private static JsonSchema LoadSchema()
    {
        const string resourceName = "InSpectra.Gen.Engine.OpenCli.Schema.OpenCli.draft.json";

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource `{resourceName}` was not found.");
        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
    }
}
