namespace OpenCli.Renderer.Models;

public sealed class NormalizedCliDocument
{
    public required OpenCliDocument Source { get; init; }

    public required IReadOnlyList<OpenCliArgument> RootArguments { get; init; }

    public required IReadOnlyList<OpenCliOption> RootOptions { get; init; }

    public required IReadOnlyList<NormalizedCommand> Commands { get; init; }
}

public sealed class NormalizedCommand
{
    public required string Path { get; init; }

    public required OpenCliCommand Command { get; init; }

    public required IReadOnlyList<OpenCliArgument> Arguments { get; init; }

    public required IReadOnlyList<OpenCliOption> DeclaredOptions { get; init; }

    public required IReadOnlyList<ResolvedOption> InheritedOptions { get; init; }

    public required IReadOnlyList<NormalizedCommand> Commands { get; init; }
}

public sealed class ResolvedOption
{
    public required OpenCliOption Option { get; init; }

    public required bool IsInherited { get; init; }

    public string? InheritedFromPath { get; init; }
}
