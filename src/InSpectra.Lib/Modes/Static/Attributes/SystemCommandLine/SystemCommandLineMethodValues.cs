namespace InSpectra.Lib.Modes.Static.Attributes.SystemCommandLine;

using InSpectra.Lib.Modes.Static.Metadata;

internal abstract class MethodValue;

internal sealed class StringValue(string? value) : MethodValue
{
    public string? Value { get; } = value;
}

internal sealed class Int32Value(int value) : MethodValue
{
    public int Value { get; } = value;
}

internal sealed class StringArrayValue(int length) : MethodValue
{
    public StringArrayValue(string?[] values)
        : this(values.Length)
    {
        for (var index = 0; index < values.Length; index++)
        {
            Values[index] = values[index];
        }
    }

    public string?[] Values { get; } = new string?[length];
}

internal sealed class OptionValue(StaticOptionDefinition definition) : MethodValue
{
    public StaticOptionDefinition Definition { get; set; } = definition;
}

internal sealed class ArgumentValue(StaticValueDefinition definition) : MethodValue
{
    public StaticValueDefinition Definition { get; set; } = definition;
}

internal sealed class CommandValue(string? displayName, bool isDefault, string? description) : MethodValue
{
    private readonly List<CommandValue> _children = [];
    private readonly Dictionary<string, OptionValue> _options = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ArgumentValue> _values = [];

    public string? DisplayName { get; } = displayName;

    public bool IsDefault { get; } = isDefault;

    public string? Description { get; } = description;

    public string? FullKey { get; private set; } = isDefault ? string.Empty : displayName;

    public void AttachTo(CommandValue parent)
    {
        if (IsDefault || string.IsNullOrWhiteSpace(DisplayName))
        {
            return;
        }

        parent.AddChild(this);
        RefreshFullKey(parent.FullKey);
    }

    public IEnumerable<CommandValue> EnumerateSelfAndDescendants()
    {
        yield return this;

        foreach (var child in _children)
        {
            foreach (var descendant in child.EnumerateSelfAndDescendants())
            {
                yield return descendant;
            }
        }
    }

    public void UpsertOption(OptionValue option)
    {
        var key = option.Definition.LongName
            ?? (option.Definition.ShortName is char shortName ? shortName.ToString() : null)
            ?? "value";
        if (!_options.ContainsKey(key))
        {
            _options[key] = option;
        }
    }

    public void AddValue(ArgumentValue value)
    {
        if (_values.Any(existing => string.Equals(existing.Definition.Name, value.Definition.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _values.Add(value);
    }

    public StaticCommandDefinition ToDefinition()
        => new(
            DisplayName,
            Description,
            IsDefault,
            IsHidden: false,
            _values.Select(static (value, index) => value.Definition with { Index = index }).ToArray(),
            _options.Values
                .Select(static option => option.Definition)
                .OrderBy(static option => option.LongName ?? option.ShortName?.ToString())
                .ToArray());

    private void AddChild(CommandValue child)
    {
        if (!_children.Contains(child))
        {
            _children.Add(child);
        }
    }

    private void RefreshFullKey(string? parentKey)
    {
        FullKey = string.IsNullOrWhiteSpace(parentKey)
            ? DisplayName
            : parentKey + " " + DisplayName;

        foreach (var child in _children)
        {
            child.RefreshFullKey(FullKey);
        }
    }
}

internal sealed class NullValue : MethodValue
{
    public static NullValue Instance { get; } = new();
}

internal sealed class CurrentMethodInstanceValue : MethodValue
{
    public static CurrentMethodInstanceValue Instance { get; } = new();
}

internal sealed class UnknownValue : MethodValue
{
    public static UnknownValue Instance { get; } = new();
}
