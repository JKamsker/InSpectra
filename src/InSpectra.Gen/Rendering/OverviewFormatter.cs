using InSpectra.Gen.Rendering.Pipeline.Model;

namespace InSpectra.Gen.Rendering;

public sealed class OverviewFormatter
{
    private static readonly Dictionary<string, string> AreaLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["auth"] = "authentication",
        ["items"] = "items",
        ["library"] = "library management",
        ["server"] = "server administration",
        ["users"] = "users",
        ["playlists"] = "playlists",
        ["sessions"] = "sessions",
        ["plugins"] = "plugins",
        ["packages"] = "packages",
        ["livetv"] = "Live TV",
        ["syncplay"] = "SyncPlay",
        ["raw"] = "raw endpoint access",
        ["devices"] = "devices",
        ["collections"] = "collections",
        ["backups"] = "backups",
        ["artists"] = "artists",
        ["genres"] = "genres",
        ["persons"] = "people",
        ["studios"] = "studios",
        ["tasks"] = "scheduled tasks",
    };

    private static readonly Dictionary<string, int> AreaPriority = new(StringComparer.OrdinalIgnoreCase)
    {
        ["auth"] = 0,
        ["items"] = 1,
        ["library"] = 2,
        ["server"] = 3,
        ["users"] = 4,
        ["playlists"] = 5,
        ["sessions"] = 6,
        ["plugins"] = 7,
        ["packages"] = 8,
        ["livetv"] = 9,
        ["syncplay"] = 10,
        ["raw"] = 11,
    };

    public string? BuildSummary(NormalizedCliDocument document, string? titleOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(document.Source.Info.Summary))
        {
            return document.Source.Info.Summary;
        }

        var commandCount = CountCommands(document.Commands);
        if (commandCount == 0)
        {
            return null;
        }

        var areas = BuildAreaLabels(document.Commands).Take(6).ToArray();
        if (IsLikelyJellyfin(document))
        {
            return CombineSentences(
                "Manage your Jellyfin server from the command line.",
                BuildAreaSentence(areas, document.Commands.Count > areas.Length));
        }

        var displayTitle = string.IsNullOrWhiteSpace(titleOverride)
            ? document.Source.Info.Title
            : titleOverride;
        var title = string.IsNullOrWhiteSpace(displayTitle)
            ? "Command-line reference."
            : $"Command-line reference for `{displayTitle}`.";
        return CombineSentences(title, BuildAreaSentence(areas, document.Commands.Count > areas.Length));
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

    private static IEnumerable<string> BuildAreaLabels(IEnumerable<NormalizedCommand> commands)
    {
        return commands
            .Select((command, index) => new
            {
                Label = GetAreaLabel(command.Command.Name),
                Priority = AreaPriority.GetValueOrDefault(command.Command.Name, 100),
                Weight = CountCommands(command.Commands),
                Index = index,
            })
            .OrderBy(item => item.Priority)
            .ThenByDescending(item => item.Weight)
            .ThenBy(item => item.Index)
            .Select(item => item.Label);
    }

    private static string GetAreaLabel(string commandName)
    {
        if (AreaLabels.TryGetValue(commandName, out var label))
        {
            return label;
        }

        return commandName.Replace('-', ' ');
    }

    private static string BuildAreaSentence(IReadOnlyList<string> areas, bool truncated)
    {
        if (areas.Count == 0)
        {
            return string.Empty;
        }

        return truncated
            ? $"Available command areas include {string.Join(", ", areas)}, and more."
            : $"Available command areas include {FormatHumanList(areas)}.";
    }

    private static string FormatHumanList(IReadOnlyList<string> items)
    {
        return items.Count switch
        {
            0 => string.Empty,
            1 => items[0],
            2 => $"{items[0]} and {items[1]}",
            _ => $"{string.Join(", ", items.Take(items.Count - 1))}, and {items[^1]}",
        };
    }

    private static bool IsLikelyJellyfin(NormalizedCliDocument document)
    {
        return EnumerateText(document).Any(text => text.Contains("Jellyfin", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> EnumerateText(NormalizedCliDocument document)
    {
        if (!string.IsNullOrWhiteSpace(document.Source.Info.Title))
        {
            yield return document.Source.Info.Title;
        }

        if (!string.IsNullOrWhiteSpace(document.Source.Info.Description))
        {
            yield return document.Source.Info.Description;
        }

        foreach (var command in document.Commands)
        {
            foreach (var text in EnumerateText(command))
            {
                yield return text;
            }
        }
    }

    private static IEnumerable<string> EnumerateText(NormalizedCommand command)
    {
        yield return command.Command.Name;

        if (!string.IsNullOrWhiteSpace(command.Command.Description))
        {
            yield return command.Command.Description;
        }

        foreach (var child in command.Commands)
        {
            foreach (var text in EnumerateText(child))
            {
                yield return text;
            }
        }
    }

    private static string CombineSentences(string lead, string tail)
    {
        return string.IsNullOrWhiteSpace(tail) ? lead : $"{lead} {tail}";
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
