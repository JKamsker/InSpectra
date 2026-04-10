namespace InSpectra.Gen.Acquisition.Tests;

using InSpectra.Discovery.Tool.Help.Artifacts;
using InSpectra.Discovery.Tool.Infrastructure.Paths;

using System.Text.Json.Nodes;

public sealed class CrawlArtifactRegeneratorFailureTests
{
    [Fact]
    public void Regenerator_Prefers_Stored_Single_Stream_Help_Payload_Over_Invocation_Echo_Combination()
    {
        using var repository = RepositoryRegressionTestSupport.CreateRepository();
        var repositoryRoot = repository.Path;
        var versionRoot = Path.Combine(repositoryRoot, "index", "packages", "spriggit.yaml.skyrim", "0.41.0-alpha.5");

        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(versionRoot, "metadata.json"),
            new JsonObject
            {
                ["schemaVersion"] = 1,
                ["packageId"] = "spriggit.yaml.skyrim",
                ["version"] = "0.41.0-alpha.5",
                ["command"] = "Spriggit.Yaml.Skyrim",
                ["cliFramework"] = "CommandLineParser",
                ["analysisMode"] = "help",
                ["status"] = "partial",
                ["steps"] = new JsonObject
                {
                    ["opencli"] = new JsonObject
                    {
                        ["classification"] = "invalid-opencli-artifact",
                    },
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
                        ["helpInvocation"] = "--help",
                        ["payload"] =
                            """
                            Spriggit version 0.41.0
                            --help
                            Spriggit.Yaml.Skyrim 0.41.0
                            2024

                              serialize, convert-from-plugin    Converts a plugin to text.

                              help                              Display more information on a specific command.
                            """,
                        ["result"] = new JsonObject
                        {
                            ["stdout"] =
                                """
                                Spriggit version 0.41.0
                                --help
                                """,
                            ["stderr"] =
                                """
                                Spriggit.Yaml.Skyrim 0.41.0
                                2024

                                  serialize, convert-from-plugin    Converts a plugin to text.

                                  help                              Display more information on a specific command.
                                """,
                            ["exitCode"] = 1,
                            ["timedOut"] = false,
                            ["status"] = "failed",
                            ["durationMs"] = 1,
                        },
                    },
                    new JsonObject
                    {
                        ["command"] = "convert-from-plugin",
                        ["helpInvocation"] = "convert-from-plugin --help",
                        ["payload"] =
                            """
                            Spriggit.Yaml.Skyrim 0.41.0

                              -i, --InputPath    Required. Path to the plugin.
                            """,
                    },
                },
            });

        var regenerator = new CrawlArtifactRegenerator();
        var result = regenerator.RegenerateRepository(repositoryRoot);

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.RewrittenCount);

        var regenerated = RepositoryRegressionTestSupport.ParseJsonObject(Path.Combine(versionRoot, "opencli.json"));
        Assert.Equal("Spriggit.Yaml.Skyrim", regenerated["info"]?["title"]?.GetValue<string>());
        Assert.Contains(
            regenerated["commands"]!.AsArray(),
            command => command?["name"]?.GetValue<string>() == "convert-from-plugin");
    }

    [Fact]
    public void Regenerator_Rejects_Unparseable_Root_Capture_As_Invalid_OpenCli()
    {
        using var repository = RepositoryRegressionTestSupport.CreateRepository();
        var repositoryRoot = repository.Path;
        var versionRoot = Path.Combine(repositoryRoot, "index", "packages", "antlr4codegenerator.tool", "2.3.0");

        RepositoryRegressionTestSupport.WriteMetadata(
            versionRoot,
            "antlr4codegenerator.tool",
            "2.3.0",
            "antlr4cg");
        RepositoryRegressionTestSupport.WriteCrawl(
            versionRoot,
            (null,
                """
                Executing: java -jar /tmp/tool/antlr-4.13.1-complete.jar --help
                Error: error(2):  unknown command-line option --help
                """));
        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(versionRoot, "opencli.json"),
            new JsonObject
            {
                ["opencli"] = "0.1-draft",
                ["x-inspectra"] = new JsonObject
                {
                    ["artifactSource"] = "crawled-from-help",
                },
                ["commands"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "Error: error(2):",
                        ["hidden"] = false,
                    },
                },
            });

        var regenerator = new CrawlArtifactRegenerator();
        var result = regenerator.RegenerateRepository(repositoryRoot);

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.RewrittenCount);
        Assert.Equal(0, result.FailedCount);
        Assert.False(File.Exists(Path.Combine(versionRoot, "opencli.json")));

        var metadata = RepositoryRegressionTestSupport.ParseJsonObject(Path.Combine(versionRoot, "metadata.json"));
        Assert.Equal("partial", metadata["status"]?.GetValue<string>());
        Assert.Null(metadata["artifacts"]?["opencliPath"]);
        Assert.Equal("invalid-opencli-artifact", metadata["steps"]?["opencli"]?["classification"]?.GetValue<string>());
    }

    [Fact]
    public void Regenerator_Rejects_Interactive_Error_Output_As_Invalid_OpenCli()
    {
        using var repository = RepositoryRegressionTestSupport.CreateRepository();
        var repositoryRoot = repository.Path;
        var versionRoot = Path.Combine(repositoryRoot, "index", "packages", "b2cconsoleclient", "1.0.0");

        RepositoryRegressionTestSupport.WriteMetadata(
            versionRoot,
            "b2cconsoleclient",
            "1.0.0",
            "b2cconsoleclient");
        RepositoryRegressionTestSupport.WriteCrawl(
            versionRoot,
            (null,
                """
                Azure B2C Console Client
                ========================
                Configuration is missing or incomplete. Let's set it up:

                Error: The authority (including the tenant ID) must be in a well-formed URI format.  (Parameter 'authority')
                Details: System.ArgumentException: The authority (including the tenant ID) must be in a well-formed URI format.  (Parameter 'authority')
                   at B2CConsoleClient.AuthenticationService..ctor(AuthConfig config) in /Users/test/B2CConsoleClient/AuthenticationService.cs:line 31
                   at B2CConsoleClient.Program.Main(String[] args) in /Users/test/B2CConsoleClient/Program.cs:line 19

                Press any key to exit...
                Unhandled exception. System.InvalidOperationException: Cannot read keys when either application does not have a console or when console input has been redirected. Try Console.Read.
                """));
        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(versionRoot, "opencli.json"),
            new JsonObject
            {
                ["opencli"] = "0.1-draft",
                ["x-inspectra"] = new JsonObject
                {
                    ["artifactSource"] = "crawled-from-help",
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

        var regenerator = new CrawlArtifactRegenerator();
        var result = regenerator.RegenerateRepository(repositoryRoot);

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.RewrittenCount);
        Assert.False(File.Exists(Path.Combine(versionRoot, "opencli.json")));

        var metadata = RepositoryRegressionTestSupport.ParseJsonObject(Path.Combine(versionRoot, "metadata.json"));
        Assert.Equal("partial", metadata["status"]?.GetValue<string>());
        Assert.Equal("invalid-output", metadata["introspection"]?["opencli"]?["status"]?.GetValue<string>());
        Assert.Equal("invalid-opencli-artifact", metadata["introspection"]?["opencli"]?["classification"]?.GetValue<string>());
    }
}
