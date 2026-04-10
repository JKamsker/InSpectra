namespace InSpectra.Gen.Acquisition.Tests;

using InSpectra.Discovery.Tool.Infrastructure.Host;
using InSpectra.Discovery.Tool.Infrastructure.Paths;

using System.Text.Json.Nodes;

internal static class RepositoryRegressionTestSupport
{
    public static TemporaryDirectory CreateRepository()
    {
        Runtime.Initialize();

        var tempDirectory = new TemporaryDirectory();
        RepositoryPathResolver.WriteTextFile(
            Path.Combine(tempDirectory.Path, "InSpectra.Gen.sln"),
            string.Empty);
        return tempDirectory;
    }

    public static JsonObject ParseJsonObject(string path)
        => JsonNode.Parse(File.ReadAllText(path))?.AsObject()
            ?? throw new InvalidOperationException($"Expected JSON object at '{path}'.");

    public static void WriteMetadata(
        string versionRoot,
        string packageId,
        string version,
        string command,
        string? cliFramework = null,
        string? artifactSource = null,
        string? openCliPath = null,
        string? crawlPath = null)
    {
        var metadata = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["packageId"] = packageId,
            ["version"] = version,
            ["command"] = command,
            ["cliFramework"] = cliFramework,
        };

        if (!string.IsNullOrWhiteSpace(artifactSource))
        {
            metadata["steps"] = new JsonObject
            {
                ["opencli"] = new JsonObject
                {
                    ["artifactSource"] = artifactSource,
                },
            };
        }

        if (!string.IsNullOrWhiteSpace(openCliPath) || !string.IsNullOrWhiteSpace(crawlPath))
        {
            metadata["artifacts"] = new JsonObject
            {
                ["opencliPath"] = openCliPath,
                ["crawlPath"] = crawlPath,
            };
        }

        RepositoryPathResolver.WriteJsonFile(Path.Combine(versionRoot, "metadata.json"), metadata);
    }

    public static void WriteCrawl(string versionRoot, params (string? Command, string Payload)[] captures)
    {
        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(versionRoot, "crawl.json"),
            new JsonObject
            {
                ["commands"] = new JsonArray(
                    captures.Select(capture =>
                        (JsonNode)new JsonObject
                        {
                            ["command"] = capture.Command,
                            ["payload"] = capture.Payload,
                        }).ToArray()),
            });
    }

    internal sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"inspectra-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
