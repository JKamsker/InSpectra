using InSpectra.Gen.Rendering.Pipeline.Model;
using InSpectra.Gen.Rendering.Contracts;

namespace InSpectra.Gen.Rendering.Pipeline;

public sealed class RenderStatsFactory
{
    public RenderStats Create(NormalizedCliDocument document, int fileCount)
    {
        return new RenderStats(
            CountCommands(document.Commands),
            CountOptions(document),
            CountArguments(document),
            fileCount);
    }

    private static int CountCommands(IEnumerable<NormalizedCommand> commands)
    {
        return commands.Sum(command => 1 + CountCommands(command.Commands));
    }

    private static int CountOptions(NormalizedCliDocument document)
    {
        return document.RootOptions.Count + CountOptions(document.Commands);
    }

    private static int CountOptions(IEnumerable<NormalizedCommand> commands)
    {
        return commands.Sum(command => command.DeclaredOptions.Count + CountOptions(command.Commands));
    }

    private static int CountArguments(NormalizedCliDocument document)
    {
        return document.RootArguments.Count + CountArguments(document.Commands) + CountOptionArguments(document.RootOptions);
    }

    private static int CountArguments(IEnumerable<NormalizedCommand> commands)
    {
        return commands.Sum(command =>
            command.Arguments.Count +
            CountOptionArguments(command.DeclaredOptions) +
            CountArguments(command.Commands));
    }

    private static int CountOptionArguments(IEnumerable<OpenCliOption> options)
    {
        return options.Sum(option => option.Arguments.Count);
    }
}
