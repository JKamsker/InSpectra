namespace InSpectra.Gen.Acquisition.StaticAnalysis.Attributes.SystemCommandLine;

using InSpectra.Gen.Acquisition.StaticAnalysis.Models;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class SystemCommandLineConstructorOperationSupport
{
    public static bool TryReadArgumentValue(MethodDef constructor, Instruction instruction, out ConstructorValue value)
    {
        if (instruction.OpCode.Code is not (
                Code.Ldarg
                or Code.Ldarg_S
                or Code.Ldarg_0
                or Code.Ldarg_1
                or Code.Ldarg_2
                or Code.Ldarg_3))
        {
            value = ConstructorUnknownValue.Instance;
            return false;
        }

        if (!SystemCommandLineInstructionSupport.TryGetArgumentIndex(constructor, instruction, out var argumentIndex))
        {
            value = ConstructorUnknownValue.Instance;
            return false;
        }

        value = argumentIndex == 0 && constructor.MethodSig?.HasThis == true
            ? CurrentCommandValue.Instance
            : ConstructorUnknownValue.Instance;
        return true;
    }

    public static ConstructorValue BuildArrayValue(List<ConstructorValue> stack, ITypeDefOrRef? elementType)
    {
        if (!string.Equals(elementType?.FullName, "System.String", StringComparison.Ordinal))
        {
            Pop(stack);
            return ConstructorUnknownValue.Instance;
        }

        return Pop(stack) is ConstructorInt32Value length && length.Value >= 0
            ? new ConstructorStringArrayValue(length.Value)
            : ConstructorUnknownValue.Instance;
    }

    public static void ApplyArrayElementAssignment(List<ConstructorValue> stack)
    {
        var value = Pop(stack);
        var index = Pop(stack) as ConstructorInt32Value;
        var array = Pop(stack) as ConstructorStringArrayValue;
        if (array is null || index is null || value is not ConstructorStringValue stringValue)
        {
            return;
        }

        if (index.Value >= 0 && index.Value < array.Values.Length)
        {
            array.Values[index.Value] = stringValue.Value;
        }
    }

    public static void ApplyFieldStore(
        List<ConstructorValue> stack,
        IField? field,
        IDictionary<string, ConstructorValue> fields)
    {
        var value = ApplyMemberIdentity(Pop(stack), field?.Name);
        var target = Pop(stack);
        if (field is null || target is not CurrentCommandValue)
        {
            return;
        }

        fields[GetFieldKey(field)] = value;
    }

    public static void ApplyFieldLoad(
        List<ConstructorValue> stack,
        IField? field,
        IReadOnlyDictionary<string, ConstructorValue> fields)
    {
        var target = Pop(stack);
        if (field is null || target is not CurrentCommandValue)
        {
            stack.Add(ConstructorUnknownValue.Instance);
            return;
        }

        stack.Add(fields.TryGetValue(GetFieldKey(field), out var value)
            ? ApplyMemberIdentity(value, field.Name)
            : ConstructorUnknownValue.Instance);
    }

    public static void ApplyStaticFieldStore(
        List<ConstructorValue> stack,
        IField? field,
        IDictionary<string, ConstructorValue> fields)
    {
        if (field is null)
        {
            Pop(stack);
            return;
        }

        fields[GetFieldKey(field)] = ApplyMemberIdentity(Pop(stack), field.Name);
    }

    public static void ApplyStaticFieldLoad(
        List<ConstructorValue> stack,
        IField? field,
        IReadOnlyDictionary<string, ConstructorValue> fields)
    {
        if (field is null)
        {
            stack.Add(ConstructorUnknownValue.Instance);
            return;
        }

        stack.Add(fields.TryGetValue(GetFieldKey(field), out var value)
            ? ApplyMemberIdentity(value, field.Name)
            : ConstructorUnknownValue.Instance);
    }

    public static string GetFieldKey(IField field)
        => field.FullName;

    public static void UpsertOption(IDictionary<string, ConstructorOptionValue> options, ConstructorOptionValue optionValue)
    {
        var definition = optionValue.Definition;
        var key = definition.LongName
            ?? (definition.ShortName is char shortName ? shortName.ToString() : null)
            ?? "value";
        if (!options.ContainsKey(key))
        {
            options[key] = optionValue;
        }
    }

    public static (string? LongName, char? ShortName) ReadOptionAliases(ConstructorValue? value)
    {
        var aliases = value switch
        {
            ConstructorStringValue stringValue => [stringValue.Value],
            ConstructorStringArrayValue arrayValue => arrayValue.Values,
            _ => [],
        };
        var longName = aliases
            .FirstOrDefault(static alias => IsLongAlias(alias))
            ?? aliases.FirstOrDefault(static alias => !string.IsNullOrWhiteSpace(alias) && !IsShortAlias(alias));
        var shortAlias = aliases.FirstOrDefault(static alias => IsShortAlias(alias));
        return (NormalizeOptionName(longName), shortAlias is { Length: 2 } ? shortAlias[1] : null);
    }

    public static string? NormalizeArgumentName(string? name)
        => string.IsNullOrWhiteSpace(name)
            ? null
            : name.Trim().Trim('<', '>', '[', ']', '(', ')');

    public static bool IsArgumentAttachMethod(IMethod method)
        => string.Equals(method.Name, "AddArgument", StringComparison.Ordinal)
            || string.Equals(method.Name, "Add", StringComparison.Ordinal)
                && method.MethodSig?.Params.FirstOrDefault() is { } parameter
                && SystemCommandLineTypeHierarchySupport.IsArgumentType(parameter.ToTypeDefOrRef());

    public static bool IsOptionAttachMethod(IMethod method)
        => string.Equals(method.Name, "AddOption", StringComparison.Ordinal)
            || string.Equals(method.Name, "Add", StringComparison.Ordinal)
                && method.MethodSig?.Params.FirstOrDefault() is { } parameter
                && SystemCommandLineTypeHierarchySupport.IsOptionType(parameter.ToTypeDefOrRef());

    public static ConstructorValue[] PopArguments(List<ConstructorValue> stack, int count)
    {
        var values = new ConstructorValue[count];
        for (var index = count - 1; index >= 0; index--)
        {
            values[index] = Pop(stack);
        }

        return values;
    }

    public static ConstructorValue Pop(List<ConstructorValue> stack)
    {
        if (stack.Count == 0)
        {
            return ConstructorUnknownValue.Instance;
        }

        var value = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return value;
    }

    private static bool IsLongAlias(string? alias)
        => !string.IsNullOrWhiteSpace(alias)
            && alias.Length > 2
            && (alias.StartsWith("--", StringComparison.Ordinal)
                || alias.StartsWith("-", StringComparison.Ordinal)
                || alias.StartsWith("/", StringComparison.Ordinal));

    private static bool IsShortAlias(string? alias)
        => !string.IsNullOrWhiteSpace(alias)
            && alias.Length == 2
            && (alias[0] == '-' || alias[0] == '/');

    private static string? NormalizeOptionName(string? alias)
        => string.IsNullOrWhiteSpace(alias)
            ? null
            : alias.Trim().TrimStart('-', '/');

    private static ConstructorValue ApplyMemberIdentity(ConstructorValue value, string? memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName))
        {
            return value;
        }

        if (value is ConstructorOptionValue optionValue
            && string.IsNullOrWhiteSpace(optionValue.Definition.PropertyName))
        {
            optionValue.Definition = optionValue.Definition with { PropertyName = memberName };
        }

        return value;
    }
}
