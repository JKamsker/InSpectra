namespace InSpectra.Gen.Engine.Modes.Static.Attributes.SystemCommandLine;

using InSpectra.Gen.Engine.Modes.Static.Metadata;

internal static class SystemCommandLineAttributeMergeSupport
{
    public static StaticCommandDefinition CreateCommandDefinition(
        string? name,
        string? description,
        bool isRoot,
        IReadOnlyList<StaticValueDefinition> values,
        IReadOnlyList<StaticOptionDefinition> options)
        => new(
            Name: name,
            Description: description,
            IsDefault: isRoot,
            IsHidden: false,
            Values: MergeValues(values),
            Options: MergeOptions(options));

    public static StaticCommandDefinition Merge(StaticCommandDefinition existing, StaticCommandDefinition candidate)
        => new(
            Name: existing.Name ?? candidate.Name,
            Description: candidate.Description ?? existing.Description,
            IsDefault: existing.IsDefault || candidate.IsDefault,
            IsHidden: existing.IsHidden && candidate.IsHidden,
            Values: MergeValues(existing.Values.Concat(candidate.Values).ToArray()),
            Options: MergeOptions(existing.Options.Concat(candidate.Options).ToArray()));

    private static StaticOptionDefinition[] MergeOptions(IReadOnlyList<StaticOptionDefinition> options)
        => MergeShortOnlyMemberStubOptions(
            options
                .Select(static option => option)
                .GroupBy(BuildOptionMergeKey, StringComparer.OrdinalIgnoreCase)
                .Select(static group => group
                    .Aggregate(MergeOptionDefinitions))
                .ToArray())
            .OrderBy(static option => option.LongName ?? option.ShortName?.ToString())
            .ToArray();

    private static StaticValueDefinition[] MergeValues(IReadOnlyList<StaticValueDefinition> values)
        => values
            .Select(static value => value)
            .GroupBy(static candidate => candidate.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group
                .Aggregate(MergeValueDefinitions))
            .Select(static (value, index) => value with { Index = index })
            .ToArray();

    private static string BuildOptionMergeKey(StaticOptionDefinition option)
        => option.LongName
            ?? (option.ShortName is char shortName ? shortName.ToString() : null)
            ?? option.PropertyName
            ?? string.Empty;

    private static StaticOptionDefinition MergeOptionDefinitions(StaticOptionDefinition left, StaticOptionDefinition right)
    {
        var preferred = Score(left) >= Score(right) ? left : right;
        var fallback = preferred == left ? right : left;
        return preferred with
        {
            LongName = SelectMergedLongName(preferred, fallback),
            ShortName = preferred.ShortName ?? fallback.ShortName,
            IsRequired = preferred.IsRequired || fallback.IsRequired,
            IsSequence = preferred.IsSequence || fallback.IsSequence,
            IsBoolLike = preferred.IsBoolLike || fallback.IsBoolLike,
            ClrType = preferred.ClrType ?? fallback.ClrType,
            Description = PickPreferredText(preferred.Description, fallback.Description),
            DefaultValue = preferred.DefaultValue ?? fallback.DefaultValue,
            MetaValue = preferred.MetaValue ?? fallback.MetaValue,
            AcceptedValues = MergeAcceptedValues(preferred.AcceptedValues, fallback.AcceptedValues),
            PropertyName = preferred.PropertyName ?? fallback.PropertyName,
        };
    }

    private static string? SelectMergedLongName(StaticOptionDefinition preferred, StaticOptionDefinition fallback)
    {
        var preferredLongName = ShouldDiscardSyntheticLongName(preferred, fallback)
            ? null
            : preferred.LongName;
        var fallbackLongName = ShouldDiscardSyntheticLongName(fallback, preferred)
            ? null
            : fallback.LongName;
        return preferredLongName ?? fallbackLongName;
    }

    private static bool ShouldDiscardSyntheticLongName(StaticOptionDefinition candidate, StaticOptionDefinition counterpart)
    {
        if (candidate.ShortName is not null
            || counterpart.LongName is not null
            || counterpart.ShortName is null
            || string.IsNullOrWhiteSpace(candidate.LongName)
            || string.IsNullOrWhiteSpace(candidate.PropertyName))
        {
            return false;
        }

        return string.Equals(
            candidate.LongName,
            SystemCommandLineAttributeReader.BuildMemberDerivedOptionName(candidate.PropertyName),
            StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<StaticOptionDefinition> MergeShortOnlyMemberStubOptions(IReadOnlyList<StaticOptionDefinition> options)
    {
        var merged = options.ToList();
        for (var index = 0; index < merged.Count; index++)
        {
            var shortOnlyOption = merged[index];
            if (shortOnlyOption.LongName is not null
                || shortOnlyOption.ShortName is null
                || string.IsNullOrWhiteSpace(shortOnlyOption.PropertyName))
            {
                continue;
            }

            var stubIndex = merged.FindIndex(candidate =>
                !ReferenceEquals(candidate, shortOnlyOption)
                && string.Equals(candidate.PropertyName, shortOnlyOption.PropertyName, StringComparison.OrdinalIgnoreCase)
                && ShouldDiscardSyntheticLongName(candidate, shortOnlyOption));
            if (stubIndex < 0)
            {
                continue;
            }

            shortOnlyOption = MergeOptionDefinitions(shortOnlyOption, merged[stubIndex]);
            merged[index] = shortOnlyOption;
            merged.RemoveAt(stubIndex);
            if (stubIndex < index)
            {
                index--;
            }
        }

        return merged;
    }

    private static StaticValueDefinition MergeValueDefinitions(StaticValueDefinition left, StaticValueDefinition right)
    {
        var preferred = Score(left) >= Score(right) ? left : right;
        var fallback = preferred == left ? right : left;
        return preferred with
        {
            Name = preferred.Name ?? fallback.Name,
            IsRequired = preferred.IsRequired || fallback.IsRequired,
            IsSequence = preferred.IsSequence || fallback.IsSequence,
            ClrType = preferred.ClrType ?? fallback.ClrType,
            Description = PickPreferredText(preferred.Description, fallback.Description),
            DefaultValue = preferred.DefaultValue ?? fallback.DefaultValue,
            AcceptedValues = MergeAcceptedValues(preferred.AcceptedValues, fallback.AcceptedValues),
        };
    }

    private static string? PickPreferredText(string? preferred, string? fallback)
    {
        if (string.IsNullOrWhiteSpace(preferred))
        {
            return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
        }

        if (string.IsNullOrWhiteSpace(fallback))
        {
            return preferred;
        }

        return preferred.Length >= fallback.Length
            ? preferred
            : fallback;
    }

    private static IReadOnlyList<string> MergeAcceptedValues(IReadOnlyList<string> preferred, IReadOnlyList<string> fallback)
        => preferred
            .Concat(fallback)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static int Score(StaticOptionDefinition option)
        => (option.LongName is null ? 0 : 8)
            + (option.ShortName is null ? 0 : 6)
            + (string.IsNullOrWhiteSpace(option.Description) ? 0 : 4)
            + (option.ClrType is null ? 0 : 2)
            + (option.AcceptedValues.Count == 0 ? 0 : 1)
            + (option.PropertyName is null ? 0 : 1)
            + (option.MetaValue is null ? 0 : 1);

    private static int Score(StaticValueDefinition value)
        => (value.Name is null ? 0 : 8)
            + (string.IsNullOrWhiteSpace(value.Description) ? 0 : 4)
            + (value.ClrType is null ? 0 : 2)
            + (value.AcceptedValues.Count == 0 ? 0 : 1);
}
