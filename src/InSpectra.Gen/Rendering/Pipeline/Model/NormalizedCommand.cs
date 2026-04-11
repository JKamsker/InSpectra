namespace InSpectra.Gen.Rendering.Pipeline.Model;

public sealed class NormalizedCommand
{
    public required string Path { get; init; }

    public required OpenCliCommand Command { get; init; }

    public required IReadOnlyList<OpenCliArgument> Arguments { get; init; }

    public required IReadOnlyList<OpenCliOption> DeclaredOptions { get; init; }

    public required IReadOnlyList<ResolvedOption> InheritedOptions { get; init; }

    public required IReadOnlyList<NormalizedCommand> Commands { get; init; }
}
