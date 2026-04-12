using InSpectra.Lib.OpenCli.Model;

namespace InSpectra.Lib.Rendering.Pipeline.Model;

internal sealed class NormalizedCliDocument
{
    public required OpenCliDocument Source { get; init; }

    public required IReadOnlyList<OpenCliArgument> RootArguments { get; init; }

    public required IReadOnlyList<OpenCliOption> RootOptions { get; init; }

    public required IReadOnlyList<NormalizedCommand> Commands { get; init; }
}
