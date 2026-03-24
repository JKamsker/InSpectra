using OpenCli.Renderer.Models;

namespace OpenCli.Renderer.Services;

public sealed class OverviewFormatter
{
    public string? BuildSummary(NormalizedCliDocument document)
    {
        if (!string.IsNullOrWhiteSpace(document.Source.Info.Summary))
        {
            return document.Source.Info.Summary;
        }

        var commandCount = CountCommands(document.Commands);
        return commandCount == 0
            ? null
            : $"{document.Commands.Count} top-level command groups and {commandCount} documented commands.";
    }

    public IReadOnlyList<(string Label, string Value)> BuildFacts(NormalizedCliDocument document)
    {
        var commandCount = CountCommands(document.Commands);
        if (commandCount == 0)
        {
            return [];
        }

        var leafCount = CountLeafCommands(document.Commands);
        var facts = new List<(string Label, string Value)>
        {
            ("Top-level command groups", document.Commands.Count.ToString()),
            ("Documented commands", commandCount.ToString()),
        };

        if (leafCount != commandCount)
        {
            facts.Add(("Leaf commands", leafCount.ToString()));
        }

        if (document.Source.Examples.Count > 0)
        {
            facts.Add(("Quick-start examples", document.Source.Examples.Count.ToString()));
        }

        return facts;
    }

    private static int CountCommands(IEnumerable<NormalizedCommand> commands)
    {
        return commands.Sum(command => 1 + CountCommands(command.Commands));
    }

    private static int CountLeafCommands(IEnumerable<NormalizedCommand> commands)
    {
        return commands.Sum(command => command.Commands.Count == 0 ? 1 : CountLeafCommands(command.Commands));
    }
}
