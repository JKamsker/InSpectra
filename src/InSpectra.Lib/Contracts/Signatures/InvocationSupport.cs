namespace InSpectra.Lib.Contracts.Signatures;

internal static class InvocationSupport
{
    public static IReadOnlyList<string[]> BuildHelpInvocations(IReadOnlyList<string> commandSegments)
    {
        var invocations = new List<string[]>
        {
            commandSegments.Concat(new[] { "--help" }).ToArray(),
            commandSegments.Concat(new[] { "-h" }).ToArray(),
            commandSegments.Concat(new[] { "-?" }).ToArray(),
            commandSegments.Concat(new[] { "--h" }).ToArray(),
            commandSegments.Concat(new[] { "/help" }).ToArray(),
            commandSegments.Concat(new[] { "/?" }).ToArray(),
        };

        invocations.AddRange(BuildKeywordHelpInvocations(commandSegments));
        invocations.Add(commandSegments.ToArray());

        return invocations
            .Distinct(new InvocationComparer())
            .ToArray();
    }

    public static string GetCommandKey(IReadOnlyList<string> commandSegments)
        => commandSegments.Count == 0 ? string.Empty : string.Join(' ', commandSegments);

    private static IEnumerable<string[]> BuildKeywordHelpInvocations(IReadOnlyList<string> commandSegments)
    {
        if (commandSegments.Count == 0)
        {
            yield return ["help"];
            yield break;
        }

        yield return (new[] { "help" }).Concat(commandSegments).ToArray();

        for (var index = 1; index < commandSegments.Count; index++)
        {
            yield return commandSegments.Take(index)
                .Concat(new[] { "help" })
                .Concat(commandSegments.Skip(index))
                .ToArray();
        }

        yield return commandSegments.Concat(new[] { "help" }).ToArray();
    }

    private sealed class InvocationComparer : IEqualityComparer<string[]>
    {
        public bool Equals(string[]? x, string[]? y)
            => x is not null && y is not null && x.SequenceEqual(y, StringComparer.OrdinalIgnoreCase);

        public int GetHashCode(string[] obj)
        {
            var hash = new HashCode();
            foreach (var item in obj)
            {
                hash.Add(item, StringComparer.OrdinalIgnoreCase);
            }

            return hash.ToHashCode();
        }
    }
}
