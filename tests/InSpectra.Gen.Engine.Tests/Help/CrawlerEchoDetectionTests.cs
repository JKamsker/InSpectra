namespace InSpectra.Gen.Engine.Tests.Help;

using InSpectra.Gen.Engine.Contracts.Documents;
using InSpectra.Gen.Engine.Modes.Help.Crawling;
using InSpectra.Gen.Engine.Tooling.Process;

public sealed class CrawlerEchoDetectionTests
{
    [Fact]
    public async Task CrawlAsync_Stops_Recursive_Alias_Expansion_When_Echo_Detected()
    {
        // Arrange: a tool that always returns the same root help regardless of subcommand.
        // Without echo detection, "entity" returns root help listing "entity, e" as children,
        // then "entity e" returns the same root help, causing "entity e e", "entity e e e", etc.
        var rootHelp =
            """
            mma-cli

            USAGE
              mma-cli [command] [options]

            COMMANDS
              entity, e    Manage entities.
              config       Manage configuration.
            """;
        var runtime = new EchoingCommandRuntime(rootHelp);
        var crawler = new Crawler(runtime);

        // Act
        var result = await crawler.CrawlAsync(
            commandPath: "mma-cli",
            rootCommandName: "mma-cli",
            workingDirectory: Environment.CurrentDirectory,
            environment: new Dictionary<string, string>(),
            timeoutSeconds: 30,
            sandboxCleanupRoot: null,
            cancellationToken: CancellationToken.None);

        // Assert: echo detection should prevent explosion. We expect a small number of
        // documents (root + entity + e + config at most), not hundreds.
        Assert.Null(result.GuardrailFailureMessage);
        Assert.True(
            result.Documents.Count <= 5,
            $"Expected at most 5 documents but got {result.Documents.Count}. " +
            $"Keys: {string.Join(", ", result.Documents.Keys.Select(k => string.IsNullOrEmpty(k) ? "<root>" : k))}");
    }

    [Fact]
    public async Task CrawlAsync_Enqueues_Children_When_Documents_Have_Different_Fingerprints()
    {
        // Arrange: a tool whose subcommands return genuinely different help documents.
        var runtime = new DistinctChildCommandRuntime();
        var crawler = new Crawler(runtime);

        // Act
        var result = await crawler.CrawlAsync(
            commandPath: "demo",
            rootCommandName: "demo",
            workingDirectory: Environment.CurrentDirectory,
            environment: new Dictionary<string, string>(),
            timeoutSeconds: 30,
            sandboxCleanupRoot: null,
            cancellationToken: CancellationToken.None);

        // Assert: both children should be crawled because their documents differ.
        Assert.Null(result.GuardrailFailureMessage);
        Assert.Contains("alpha", result.Documents.Keys);
        Assert.Contains("beta", result.Documents.Keys);
    }

    [Fact]
    public void ComputeFingerprint_Returns_Same_Value_For_Identical_Documents()
    {
        // Arrange
        var document = new Document(
            Title: "test",
            Version: null,
            ApplicationDescription: null,
            CommandDescription: null,
            UsageLines: ["test [command]"],
            Arguments: [],
            Options: [new Item("--verbose", false, "Verbose")],
            Commands: [new Item("sub1", false, "Sub 1"), new Item("sub2", false, "Sub 2")]);

        // Act
        var fingerprint1 = DocumentFingerprintSupport.ComputeFingerprint(document);
        var fingerprint2 = DocumentFingerprintSupport.ComputeFingerprint(document);

        // Assert
        Assert.Equal(fingerprint1, fingerprint2);
        Assert.True(DocumentFingerprintSupport.IsSignificantFingerprint(fingerprint1));
    }

    [Fact]
    public void ComputeFingerprint_Differs_For_Documents_With_Different_Commands()
    {
        // Arrange
        var document1 = new Document(
            Title: "test",
            Version: null,
            ApplicationDescription: null,
            CommandDescription: null,
            UsageLines: ["test [command]"],
            Arguments: [],
            Options: [new Item("--verbose", false, "Verbose")],
            Commands: [new Item("alpha", false, "Alpha"), new Item("beta", false, "Beta")]);

        var document2 = new Document(
            Title: "test",
            Version: null,
            ApplicationDescription: null,
            CommandDescription: null,
            UsageLines: ["test [command]"],
            Arguments: [],
            Options: [new Item("--verbose", false, "Verbose")],
            Commands: [new Item("gamma", false, "Gamma"), new Item("delta", false, "Delta")]);

        // Act
        var fingerprint1 = DocumentFingerprintSupport.ComputeFingerprint(document1);
        var fingerprint2 = DocumentFingerprintSupport.ComputeFingerprint(document2);

        // Assert
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public void ComputeFingerprint_Returns_Empty_For_Document_With_No_Structural_Items()
    {
        // Arrange
        var document = new Document(
            Title: "test",
            Version: null,
            ApplicationDescription: "An app",
            CommandDescription: "A command",
            UsageLines: ["test [options]"],
            Arguments: [],
            Options: [],
            Commands: []);

        // Act
        var fingerprint = DocumentFingerprintSupport.ComputeFingerprint(document);

        // Assert
        Assert.False(DocumentFingerprintSupport.IsSignificantFingerprint(fingerprint));
    }

    /// <summary>
    /// A runtime that always returns the same help text regardless of which subcommand is invoked.
    /// This simulates tools that ignore unknown subcommands and echo root help.
    /// </summary>
    private sealed class EchoingCommandRuntime(string helpText) : CommandRuntime
    {
        public override Task<ProcessResult> InvokeProcessCaptureAsync(
            string filePath,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environment,
            int timeoutSeconds,
            string? sandboxRoot,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ProcessResult(
                Status: "ok",
                TimedOut: false,
                ExitCode: 0,
                DurationMs: 1,
                Stdout: helpText,
                Stderr: string.Empty));
        }
    }

    /// <summary>
    /// A runtime where the root lists two children (alpha, beta) and each child returns
    /// a distinct help document with different options.
    /// </summary>
    private sealed class DistinctChildCommandRuntime : CommandRuntime
    {
        public override Task<ProcessResult> InvokeProcessCaptureAsync(
            string filePath,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environment,
            int timeoutSeconds,
            string? sandboxRoot,
            CancellationToken cancellationToken)
        {
            var invocation = string.Join(' ', argumentList);

            string stdout;
            if (invocation.StartsWith("alpha", StringComparison.OrdinalIgnoreCase)
                || invocation.Contains(" alpha ", StringComparison.OrdinalIgnoreCase))
            {
                stdout =
                    """
                    demo alpha

                    USAGE
                      demo alpha [options]

                    OPTIONS
                      --format  Output format.
                    """;
            }
            else if (invocation.StartsWith("beta", StringComparison.OrdinalIgnoreCase)
                     || invocation.Contains(" beta ", StringComparison.OrdinalIgnoreCase))
            {
                stdout =
                    """
                    demo beta

                    USAGE
                      demo beta [options]

                    OPTIONS
                      --count  Number of items.
                    """;
            }
            else
            {
                stdout =
                    """
                    demo

                    USAGE
                      demo [command] [options]

                    COMMANDS
                      alpha    Alpha command.
                      beta     Beta command.
                    """;
            }

            return Task.FromResult(new ProcessResult(
                Status: "ok",
                TimedOut: false,
                ExitCode: 0,
                DurationMs: 1,
                Stdout: stdout,
                Stderr: string.Empty));
        }
    }
}
