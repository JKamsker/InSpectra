using InSpectra.Gen.Services;

namespace InSpectra.Gen.Tests;

public class OpenCliCompatibilityTests
{
    private readonly OpenCliDocumentLoader _loader = new(new OpenCliSchemaProvider());

    [Fact]
    public void Loader_accepts_synthesized_noise_and_normalizes_common_array_shapes()
    {
        const string json = """
            {
              "opencli": "0.1-draft",
              "info": {
                "title": "demo",
                "version": "1.0"
              },
              "x-inspectra": {
                "synthesized": true,
                "artifactSource": "synthesized-from-xmldoc"
              },
              "options": null,
              "commands": [
                {
                  "name": "alpha",
                  "options": null,
                  "arguments": null,
                  "commands": null,
                  "examples": "alpha run",
                  "metadata": null
                }
              ],
              "examples": null,
              "metadata": null
            }
            """;

        var document = _loader.LoadFromJson(json, "synthetic");

        Assert.Empty(document.Options);
        Assert.Empty(document.Examples);

        var command = Assert.Single(document.Commands);
        Assert.Empty(command.Options);
        Assert.Empty(command.Arguments);
        Assert.Empty(command.Commands);
        Assert.Empty(command.Metadata);
        Assert.Equal(new[] { "alpha run" }, command.Examples);
    }
}
