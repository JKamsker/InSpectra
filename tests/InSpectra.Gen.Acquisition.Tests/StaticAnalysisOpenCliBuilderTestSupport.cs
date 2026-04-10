namespace InSpectra.Gen.Acquisition.Tests;

using InSpectra.Discovery.Tool.Help.Documents;

internal static class StaticAnalysisOpenCliBuilderTestSupport
{
    public static Document CreateHelpDocument(
        string? title = null,
        string? version = null,
        string? description = null,
        IReadOnlyList<Item>? options = null,
        IReadOnlyList<Item>? commands = null)
        => new(
            Title: title,
            Version: version,
            ApplicationDescription: null,
            CommandDescription: description,
            UsageLines: [],
            Arguments: [],
            Options: options ?? [],
            Commands: commands ?? []);
}
