namespace InSpectra.Gen.Engine.Tests.StaticAnalysis;

using InSpectra.Gen.Engine.Contracts.Documents;

internal static class StaticAnalysisOpenCliBuilderTestSupport
{
    public static Document CreateHelpDocument(
        string? title = null,
        string? version = null,
        string? description = null,
        IReadOnlyList<string>? usageLines = null,
        IReadOnlyList<Item>? arguments = null,
        IReadOnlyList<Item>? options = null,
        IReadOnlyList<Item>? commands = null)
        => new(
            Title: title,
            Version: version,
            ApplicationDescription: null,
            CommandDescription: description,
            UsageLines: usageLines ?? [],
            Arguments: arguments ?? [],
            Options: options ?? [],
            Commands: commands ?? []);
}
