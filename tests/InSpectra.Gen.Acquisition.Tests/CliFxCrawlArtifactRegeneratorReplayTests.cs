namespace InSpectra.Gen.Acquisition.Tests;

using InSpectra.Discovery.Tool.Analysis.CliFx.Artifacts;
using InSpectra.Discovery.Tool.Analysis.CliFx.Metadata;
using InSpectra.Discovery.Tool.Infrastructure.Paths;

using System.Text.Json.Nodes;

public sealed class CliFxCrawlArtifactRegeneratorReplayTests
{
    [Fact]
    public void Regenerates_CliFx_OpenCli_From_Stored_Crawl_And_Metadata()
    {
        using var repository = RepositoryRegressionTestSupport.CreateRepository();
        var repositoryRoot = repository.Path;
        var versionRoot = Path.Combine(repositoryRoot, "index", "packages", "sampleclifx", "1.0.0");

        RepositoryRegressionTestSupport.WriteMetadata(
            versionRoot,
            "SampleCliFx",
            "1.0.0",
            "sample",
            cliFramework: "CliFx");

        var staticCommands = new Dictionary<string, CliFxCommandDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = new(Name: null, Description: "Default command", Parameters: [], Options: []),
            ["user add"] = new(
                Name: "user add",
                Description: "Adds a user",
                Parameters: [],
                Options:
                [
                    new CliFxOptionDefinition(
                        Name: null,
                        ShortName: 's',
                        IsRequired: true,
                        IsSequence: false,
                        IsBoolLike: false,
                        ClrType: "System.String",
                        Description: "Script path",
                        EnvironmentVariable: null,
                        AcceptedValues: [],
                        ValueName: "scriptPath"),
                ]),
        };

        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(versionRoot, "crawl.json"),
            new JsonObject
            {
                ["documentCount"] = 1,
                ["captureCount"] = 1,
                ["commands"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["command"] = null,
                        ["payload"] =
                            """
                            sample 1.0.0

                            DESCRIPTION
                              Demo CLI
                            """,
                    },
                },
                ["staticCommands"] = CliFxCrawlArtifactSupport.SerializeStaticCommands(staticCommands),
            });
        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(versionRoot, "opencli.json"),
            new JsonObject
            {
                ["opencli"] = "0.1-draft",
                ["x-inspectra"] = new JsonObject
                {
                    ["artifactSource"] = "crawled-from-clifx-help",
                },
                ["commands"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "user add",
                        ["hidden"] = false,
                    },
                },
            });

        var regenerator = new CliFxCrawlArtifactRegenerator();
        var result = regenerator.RegenerateRepository(repositoryRoot);

        Assert.Equal(1, result.ScannedCount);
        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.RewrittenCount);
        Assert.Equal(0, result.FailedCount);

        var regenerated = RepositoryRegressionTestSupport.ParseJsonObject(Path.Combine(versionRoot, "opencli.json"));
        Assert.Equal("crawled-from-clifx-help", regenerated["x-inspectra"]?["artifactSource"]?.GetValue<string>());
        Assert.Equal("CliFx", regenerated["x-inspectra"]?["cliFramework"]?.GetValue<string>());

        var userCommands = Assert.IsType<JsonArray>(regenerated["commands"]);
        var user = Assert.IsType<JsonObject>(Assert.Single(userCommands));
        Assert.Equal("user", user["name"]!.GetValue<string>());
        var nestedCommands = Assert.IsType<JsonArray>(user["commands"]);
        var add = Assert.IsType<JsonObject>(Assert.Single(nestedCommands));
        var options = Assert.IsType<JsonArray>(add["options"]);
        var option = Assert.IsType<JsonObject>(Assert.Single(options));
        var arguments = Assert.IsType<JsonArray>(option["arguments"]);
        var argument = Assert.IsType<JsonObject>(Assert.Single(arguments));

        Assert.Equal("add", add["name"]!.GetValue<string>());
        Assert.Equal("-s", option["name"]!.GetValue<string>());
        Assert.Equal("SCRIPT_PATH", argument["name"]!.GetValue<string>());
    }

    [Fact]
    public void Regenerates_Legacy_CliFx_Crawl_From_Result_Payload_And_Historic_Artifact_Source()
    {
        using var repository = RepositoryRegressionTestSupport.CreateRepository();
        var repositoryRoot = repository.Path;
        var versionRoot = Path.Combine(repositoryRoot, "index", "packages", "legacyclifx", "2.0.0");

        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(versionRoot, "metadata.json"),
            new JsonObject
            {
                ["schemaVersion"] = 1,
                ["packageId"] = "LegacyCliFx",
                ["version"] = "2.0.0",
                ["command"] = "legacy",
                ["cliFramework"] = "CliFx",
                ["status"] = "partial",
                ["steps"] = new JsonObject
                {
                    ["opencli"] = new JsonObject
                    {
                        ["artifactSource"] = "crawled-from-help",
                    },
                },
                ["artifacts"] = new JsonObject
                {
                    ["metadataPath"] = "index/packages/legacyclifx/2.0.0/metadata.json",
                    ["opencliPath"] = "index/packages/legacyclifx/2.0.0/opencli.json",
                    ["opencliSource"] = "crawled-from-help",
                    ["crawlPath"] = "index/packages/legacyclifx/2.0.0/crawl.json",
                },
            });
        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(versionRoot, "crawl.json"),
            new JsonObject
            {
                ["documentCount"] = 2,
                ["captureCount"] = 2,
                ["commands"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["command"] = null,
                        ["result"] = new JsonObject
                        {
                            ["stdout"] =
                                """
                                legacy 2.0.0

                                USAGE
                                  legacy [command] [...]

                                COMMANDS
                                  upload            Upload a package
                                """,
                        },
                    },
                    new JsonObject
                    {
                        ["command"] = "upload",
                        ["result"] = new JsonObject
                        {
                            ["stdout"] =
                                """
                                legacy 2.0.0

                                USAGE
                                  legacy upload --pat <token> [--folder <path>] [options]

                                DESCRIPTION
                                  Upload a package

                                OPTIONS
                                * -p|--pat          Personal access token
                                  -f|--folder       Folder to upload
                                  -h|--help         Shows help text.
                                """,
                        },
                    },
                },
            });
        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(versionRoot, "opencli.json"),
            new JsonObject
            {
                ["opencli"] = "0.1-draft",
                ["x-inspectra"] = new JsonObject
                {
                    ["artifactSource"] = "crawled-from-help",
                    ["cliFramework"] = "CliFx",
                },
                ["commands"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "upload",
                        ["hidden"] = false,
                    },
                },
            });

        var regenerator = new CliFxCrawlArtifactRegenerator();
        var result = regenerator.RegenerateRepository(repositoryRoot);

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.RewrittenCount);
        Assert.Equal(0, result.FailedCount);

        var regenerated = RepositoryRegressionTestSupport.ParseJsonObject(Path.Combine(versionRoot, "opencli.json"));
        Assert.Equal("crawled-from-clifx-help", regenerated["x-inspectra"]?["artifactSource"]?.GetValue<string>());

        var commands = Assert.IsType<JsonArray>(regenerated["commands"]);
        var command = Assert.IsType<JsonObject>(Assert.Single(commands));
        Assert.Equal("upload", command["name"]!.GetValue<string>());
        var commandOptions = Assert.IsType<JsonArray>(command["options"]);
        var patOption = Assert.IsType<JsonObject>(commandOptions[0]);
        Assert.Equal("--pat", patOption["name"]!.GetValue<string>());
        Assert.Equal("TOKEN", patOption["arguments"]![0]!["name"]!.GetValue<string>());

        var metadata = RepositoryRegressionTestSupport.ParseJsonObject(Path.Combine(versionRoot, "metadata.json"));
        Assert.Equal("crawled-from-clifx-help", metadata["artifacts"]?["opencliSource"]?.GetValue<string>());
        Assert.True(File.Exists(Path.Combine(repositoryRoot, "index", "packages", "legacyclifx", "latest", "opencli.json")));
    }
}
