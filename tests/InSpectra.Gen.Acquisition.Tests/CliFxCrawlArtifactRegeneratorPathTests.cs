namespace InSpectra.Gen.Acquisition.Tests;

using InSpectra.Discovery.Tool.Analysis.CliFx.Artifacts;
using InSpectra.Discovery.Tool.Analysis.CliFx.Metadata;
using InSpectra.Discovery.Tool.Infrastructure.Paths;

using System.Text.Json.Nodes;

public sealed class CliFxCrawlArtifactRegeneratorPathTests
{
    [Fact]
    public void Regenerates_TitleCase_CliFx_Crawl_When_OpenCli_Is_Missing()
    {
        using var repository = RepositoryRegressionTestSupport.CreateRepository();
        var repositoryRoot = repository.Path;
        var versionRoot = Path.Combine(repositoryRoot, "index", "packages", "trainingmoduleconvertor", "0.0.9");

        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(versionRoot, "metadata.json"),
            new JsonObject
            {
                ["schemaVersion"] = 1,
                ["packageId"] = "TrainingModuleConvertor",
                ["version"] = "0.0.9",
                ["command"] = "training-module-convertor",
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
                    ["metadataPath"] = "index/packages/trainingmoduleconvertor/0.0.9/metadata.json",
                    ["opencliSource"] = "crawled-from-help",
                    ["crawlPath"] = "index/packages/trainingmoduleconvertor/0.0.9/crawl.json",
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
                                Training Modules convertor v0.0.9
                                  Training Modules convertor

                                Usage
                                  dotnet tool.dll [command] [options]

                                Options
                                  -h|--help         Shows help text.

                                Commands
                                  convert           Convert a module.
                                """,
                        },
                    },
                    new JsonObject
                    {
                        ["command"] = "convert",
                        ["result"] = new JsonObject
                        {
                            ["stdout"] =
                                """
                                Description
                                  Convert a module.

                                Usage
                                  dotnet tool.dll convert <folder> [options]

                                Parameters
                                * folder            Root folder.

                                Options
                                  -h|--help         Shows help text.
                                """,
                        },
                    },
                },
            });

        var regenerator = new CliFxCrawlArtifactRegenerator();
        var result = regenerator.RegenerateRepository(repositoryRoot);

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.RewrittenCount);
        Assert.Equal(0, result.FailedCount);
        Assert.True(File.Exists(Path.Combine(versionRoot, "opencli.json")));

        var openCli = RepositoryRegressionTestSupport.ParseJsonObject(Path.Combine(versionRoot, "opencli.json"));
        Assert.Equal("crawled-from-clifx-help", openCli["x-inspectra"]?["artifactSource"]?.GetValue<string>());
        Assert.Equal("convert", openCli["commands"]![0]!["name"]!.GetValue<string>());
    }

    [Fact]
    public void Regenerator_Drops_Unreachable_CliFx_Captures_And_Respects_Metadata_OpenCli_Path()
    {
        using var repository = RepositoryRegressionTestSupport.CreateRepository();
        var repositoryRoot = repository.Path;
        var versionRoot = Path.Combine(repositoryRoot, "index", "packages", "sampleclifx", "3.0.0");
        var redirectedOpenCliPath = Path.Combine(versionRoot, "replayed", "opencli.json");

        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(versionRoot, "metadata.json"),
            new JsonObject
            {
                ["schemaVersion"] = 1,
                ["packageId"] = "SampleCliFx",
                ["version"] = "3.0.0",
                ["command"] = "sample",
                ["cliFramework"] = "CliFx",
                ["steps"] = new JsonObject
                {
                    ["opencli"] = new JsonObject
                    {
                        ["artifactSource"] = "crawled-from-clifx-help",
                    },
                },
                ["artifacts"] = new JsonObject
                {
                    ["metadataPath"] = "index/packages/sampleclifx/3.0.0/metadata.json",
                    ["opencliPath"] = "index/packages/sampleclifx/3.0.0/replayed/opencli.json",
                    ["opencliSource"] = "crawled-from-clifx-help",
                    ["crawlPath"] = "index/packages/sampleclifx/3.0.0/crawl.json",
                },
            });
        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(versionRoot, "crawl.json"),
            new JsonObject
            {
                ["commands"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["command"] = null,
                        ["payload"] =
                            """
                            sample 3.0.0

                            USAGE
                              sample [command]

                            COMMANDS
                              upload            Upload a package
                            """,
                    },
                    new JsonObject
                    {
                        ["command"] = "upload",
                        ["payload"] =
                            """
                            sample 3.0.0

                            USAGE
                              sample upload [options]

                            DESCRIPTION
                              Upload a package

                            OPTIONS
                              --file <path>     File to upload
                            """,
                    },
                    new JsonObject
                    {
                        ["command"] = "orphan",
                        ["payload"] =
                            """
                            sample 3.0.0

                            USAGE
                              sample orphan [options]

                            DESCRIPTION
                              Should not survive replay
                            """,
                    },
                },
                ["staticCommands"] = CliFxCrawlArtifactSupport.SerializeStaticCommands(
                    new Dictionary<string, CliFxCommandDefinition>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["upload"] = new(
                            Name: "upload",
                            Description: "Upload a package",
                            Parameters: [],
                            Options: []),
                    }),
            });
        RepositoryPathResolver.WriteJsonFile(
            redirectedOpenCliPath,
            new JsonObject
            {
                ["opencli"] = "0.1-draft",
                ["x-inspectra"] = new JsonObject
                {
                    ["artifactSource"] = "crawled-from-clifx-help",
                    ["cliFramework"] = "CliFx",
                },
                ["commands"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "stale",
                        ["hidden"] = false,
                    },
                },
            });

        var regenerator = new CliFxCrawlArtifactRegenerator();
        var result = regenerator.RegenerateRepository(repositoryRoot);

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.RewrittenCount);
        Assert.False(File.Exists(Path.Combine(versionRoot, "opencli.json")));

        var regenerated = RepositoryRegressionTestSupport.ParseJsonObject(redirectedOpenCliPath);
        var commands = Assert.IsType<JsonArray>(regenerated["commands"]);
        var command = Assert.IsType<JsonObject>(Assert.Single(commands));
        Assert.Equal("upload", command["name"]!.GetValue<string>());
        Assert.DoesNotContain(
            commands,
            candidate => candidate?["name"]?.GetValue<string>() == "orphan");
    }
}
