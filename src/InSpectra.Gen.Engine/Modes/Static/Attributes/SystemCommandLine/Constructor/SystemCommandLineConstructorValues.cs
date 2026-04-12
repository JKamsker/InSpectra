namespace InSpectra.Gen.Engine.Modes.Static.Attributes.SystemCommandLine.Constructor;

using InSpectra.Gen.Engine.Modes.Static.Metadata;

internal abstract record ConstructorValue;

internal sealed record ConstructorStringValue(string? Value) : ConstructorValue;

internal sealed record ConstructorInt32Value(int Value) : ConstructorValue;

internal sealed record ConstructorStringArrayValue(int Length) : ConstructorValue
{
    public ConstructorStringArrayValue(string?[] values)
        : this(values.Length)
    {
        for (var index = 0; index < values.Length; index++)
        {
            Values[index] = values[index];
        }
    }

    public string?[] Values { get; } = new string?[Length];
}

internal sealed record ConstructorOptionValue : ConstructorValue
{
    public ConstructorOptionValue(StaticOptionDefinition definition)
    {
        Definition = definition;
    }

    public StaticOptionDefinition Definition { get; set; }
}

internal sealed record ConstructorArgumentValue : ConstructorValue
{
    public ConstructorArgumentValue(StaticValueDefinition definition)
    {
        Definition = definition;
    }

    public StaticValueDefinition Definition { get; set; }
}

internal sealed record ConstructorUnknownValue : ConstructorValue
{
    public static ConstructorUnknownValue Instance { get; } = new();
}

internal sealed record CurrentCommandValue : ConstructorValue
{
    public static CurrentCommandValue Instance { get; } = new();
}

internal sealed record ConstructorNullValue : ConstructorValue
{
    public static ConstructorNullValue Instance { get; } = new();
}
