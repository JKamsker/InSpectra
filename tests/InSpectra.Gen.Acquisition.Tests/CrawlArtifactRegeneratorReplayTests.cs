namespace InSpectra.Gen.Acquisition.Tests;

using InSpectra.Discovery.Tool.Help.Artifacts;
using InSpectra.Discovery.Tool.Infrastructure.Paths;

using System.Text.Json.Nodes;

public sealed class CrawlArtifactRegeneratorReplayTests
{
    [Fact]
    public void Regenerates_Generic_Help_OpenCli_From_Stored_Crawls()
    {
        using var repository = RepositoryRegressionTestSupport.CreateRepository();
        var repositoryRoot = repository.Path;

        var genericVersionRoot = Path.Combine(repositoryRoot, "index", "packages", "registerbot", "2.0.20");
        RepositoryRegressionTestSupport.WriteMetadata(
            genericVersionRoot,
            "RegisterBot",
            "2.0.20",
            "RegisterBot");
        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(genericVersionRoot, "crawl.json"),
            new JsonObject
            {
                ["documentCount"] = 781,
                ["commandCount"] = 781,
                ["captureCount"] = 2,
                ["commands"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["command"] = null,
                        ["payload"] =
                            """
                            RegisterBot Version 2.0.20.0

                            ```RegisterBot [--endpoint endpoint] [--name botName] [--resource-group groupName] [--help]```

                            Creates or updates a bot registration for [botName] pointing to [endpoint] with teams channel and SSO enabled.

                            | Argument                         | Description                                                                                   |
                            | -------------------------------- | --------------------------------------------------------------------------------------------- |
                            | -e, --endpoint endpoint          | (optional) If not specified the endpoint will stay the same as project settings               |
                            | -n, --name botName               | (optional) If not specified the botname will be pulled from settings or interactively asked   |
                            | -g, --resource-group groupName   | (optional) If not specified the groupname will be pulled from settings or interactively asked |
                            | -v, --verbose                    | (optional) show all commands as they are executed                                             |
                            | -h, --help                       | display help                                                                                  |
                            """,
                    },
                    new JsonObject
                    {
                        ["command"] = "| Argument",
                        ["payload"] =
                            """
                            RegisterBot Version 2.0.20.0

                            ```RegisterBot [--endpoint endpoint] [--name botName] [--resource-group groupName] [--help]```

                            Creates or updates a bot registration for [botName] pointing to [endpoint] with teams channel and SSO enabled.

                            | Argument                         | Description                                                                                   |
                            | -------------------------------- | --------------------------------------------------------------------------------------------- |
                            | -e, --endpoint endpoint          | (optional) If not specified the endpoint will stay the same as project settings               |
                            | -n, --name botName               | (optional) If not specified the botname will be pulled from settings or interactively asked   |
                            | -g, --resource-group groupName   | (optional) If not specified the groupname will be pulled from settings or interactively asked |
                            | -v, --verbose                    | (optional) show all commands as they are executed                                             |
                            | -h, --help                       | display help                                                                                  |
                            """,
                    },
                },
            });
        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(genericVersionRoot, "opencli.json"),
            new JsonObject
            {
                ["opencli"] = "0.1-draft",
                ["x-inspectra"] = new JsonObject
                {
                    ["artifactSource"] = "crawled-from-help",
                    ["helpDocumentCount"] = 781,
                },
                ["commands"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "| Argument",
                        ["hidden"] = false,
                    },
                },
            });

        var clifxVersionRoot = Path.Combine(repositoryRoot, "index", "packages", "sampleclifx", "1.0.0");
        RepositoryRegressionTestSupport.WriteMetadata(
            clifxVersionRoot,
            "SampleCliFx",
            "1.0.0",
            "sample",
            cliFramework: "CliFx");
        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(clifxVersionRoot, "crawl.json"),
            new JsonObject
            {
                ["commands"] = new JsonArray(),
            });
        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(clifxVersionRoot, "opencli.json"),
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
                        ["name"] = "sample",
                        ["hidden"] = false,
                    },
                },
            });

        var regenerator = new CrawlArtifactRegenerator();
        var result = regenerator.RegenerateRepository(repositoryRoot);

        Assert.Equal(2, result.ScannedCount);
        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.RewrittenCount);
        Assert.Equal(0, result.UnchangedCount);
        Assert.Equal(0, result.FailedCount);

        var regenerated = RepositoryRegressionTestSupport.ParseJsonObject(Path.Combine(genericVersionRoot, "opencli.json"));
        Assert.Equal("crawled-from-help", regenerated["x-inspectra"]?["artifactSource"]?.GetValue<string>());
        Assert.Equal(1, regenerated["x-inspectra"]?["helpDocumentCount"]?.GetValue<int>());
        Assert.Contains(regenerated["options"]!.AsArray(), option => option?["name"]?.GetValue<string>() == "--endpoint");
        Assert.Empty(regenerated["commands"]!.AsArray());

        var untouchedCliFx = RepositoryRegressionTestSupport.ParseJsonObject(Path.Combine(clifxVersionRoot, "opencli.json"));
        Assert.Equal("crawled-from-clifx-help", untouchedCliFx["x-inspectra"]?["artifactSource"]?.GetValue<string>());
        Assert.Single(untouchedCliFx["commands"]!.AsArray());
    }

    [Fact]
    public void Regenerator_Drops_CommandLineParser_Pseudo_Verbs_From_Crawled_Help()
    {
        using var repository = RepositoryRegressionTestSupport.CreateRepository();
        var repositoryRoot = repository.Path;
        var versionRoot = Path.Combine(repositoryRoot, "index", "packages", "dotnet-certificate-tool", "2.1.0");

        RepositoryRegressionTestSupport.WriteMetadata(
            versionRoot,
            "dotnet-certificate-tool",
            "2.1.0",
            "gsoft-cert",
            cliFramework: "CommandLineParser");
        RepositoryRegressionTestSupport.WriteCrawl(
            versionRoot,
            (null,
                """
                Error parsing
                 CommandLine.HelpVerbRequestedError
                GSoft.CertificateTool 1.0.0+bb4d252c46ae13f3169853b02995b8cd77635ab6
                Copyright (C) 2026 GSoft.CertificateTool

                  add        Installs a pfx certificate to selected store.
                  remove     Removes a pfx certificate from selected store.
                  version    Display version information.
                """),
            ("CommandLine.HelpVerbRequestedError",
                """
                Error parsing
                 CommandLine.BadVerbSelectedError
                GSoft.CertificateTool 1.0.0+bb4d252c46ae13f3169853b02995b8cd77635ab6
                """),
            ("add",
                """
                Error parsing
                 CommandLine.HelpRequestedError
                GSoft.CertificateTool 1.0.0+bb4d252c46ae13f3169853b02995b8cd77635ab6

                  -f, --file              The certificate file.
                  --help                  Display this help screen.
                """));
        RepositoryPathResolver.WriteJsonFile(
            Path.Combine(versionRoot, "opencli.json"),
            new JsonObject
            {
                ["opencli"] = "0.1-draft",
                ["x-inspectra"] = new JsonObject
                {
                    ["artifactSource"] = "crawled-from-help",
                    ["helpDocumentCount"] = 99,
                },
                ["commands"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "CommandLine.HelpVerbRequestedError",
                        ["hidden"] = false,
                    },
                },
            });

        var regenerator = new CrawlArtifactRegenerator();
        var result = regenerator.RegenerateRepository(repositoryRoot);

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.RewrittenCount);

        var regenerated = RepositoryRegressionTestSupport.ParseJsonObject(Path.Combine(versionRoot, "opencli.json"));
        Assert.Equal("GSoft.CertificateTool", regenerated["info"]?["title"]?.GetValue<string>());
        Assert.DoesNotContain(
            regenerated["commands"]!.AsArray(),
            command => command?["name"]?.GetValue<string>() == "CommandLine.HelpVerbRequestedError");
        Assert.Contains(
            regenerated["commands"]!.AsArray(),
            command => command?["name"]?.GetValue<string>() == "add" && command?["options"] is JsonArray);
    }
}
