using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using InSpectra.Gen.Models;
using InSpectra.Gen.Runtime;

namespace InSpectra.Gen.Services;

public sealed class OpenCliDocumentLoader(OpenCliSchemaProvider schemaProvider)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
    };

    public async Task<OpenCliDocument> LoadFromFileAsync(string path, CancellationToken cancellationToken)
    {
        var resolvedPath = Path.GetFullPath(path);
        if (!File.Exists(resolvedPath))
        {
            throw new CliUsageException($"OpenCLI file `{resolvedPath}` does not exist.");
        }

        var json = await File.ReadAllTextAsync(resolvedPath, cancellationToken);
        return LoadFromJson(json, resolvedPath);
    }

    public OpenCliDocument LoadFromJson(string json, string sourceLabel)
    {
        JsonNode rootNode;
        JsonNode sanitizedNode;
        string sanitizedJson;

        try
        {
            rootNode = JsonNode.Parse(json) ?? throw new CliDataException($"OpenCLI source `{sourceLabel}` is empty.");
        }
        catch (JsonException exception)
        {
            throw new CliDataException($"OpenCLI source `{sourceLabel}` is not valid JSON.", [exception.Message], exception);
        }

        sanitizedNode = OpenCliCompatibilitySanitizer.Sanitize(rootNode);
        sanitizedJson = sanitizedNode.ToJsonString();

        ValidateSchema(sanitizedNode, sourceLabel);

        try
        {
            return JsonSerializer.Deserialize<OpenCliDocument>(sanitizedJson, SerializerOptions)
                ?? throw new CliDataException($"OpenCLI source `{sourceLabel}` could not be deserialized.");
        }
        catch (JsonException exception)
        {
            throw new CliDataException($"OpenCLI source `{sourceLabel}` could not be deserialized.", [exception.Message], exception);
        }
    }

    private void ValidateSchema(JsonNode rootNode, string sourceLabel)
    {
        using var document = JsonDocument.Parse(rootNode.ToJsonString());
        var evaluation = schemaProvider.GetSchema().Evaluate(
            document.RootElement,
            new EvaluationOptions
            {
                OutputFormat = OutputFormat.List,
                RequireFormatValidation = false,
            });

        if (evaluation.IsValid)
        {
            return;
        }

        var details = FlattenErrors(evaluation)
            .Distinct(StringComparer.Ordinal)
            .Take(10)
            .ToList();

        throw new CliDataException(
            $"OpenCLI source `{sourceLabel}` failed schema validation.",
            details.Count > 0 ? details : ["The document did not match the embedded OpenCLI schema."]);
    }

    private static IEnumerable<string> FlattenErrors(EvaluationResults results)
    {
        if (results.Errors is not null)
        {
            foreach (var error in results.Errors)
            {
                var location = string.IsNullOrWhiteSpace(results.InstanceLocation.ToString())
                    ? "$"
                    : results.InstanceLocation.ToString();
                yield return $"{location}: {error.Value}";
            }
        }

        if (results.Details is null)
        {
            yield break;
        }

        foreach (var detail in results.Details)
        {
            foreach (var nested in FlattenErrors(detail))
            {
                yield return nested;
            }
        }
    }
}
