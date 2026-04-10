namespace InSpectra.Gen.Acquisition.StaticAnalysis.Attributes.SystemCommandLine;

using dnlib.DotNet.Emit;
using dnlib.DotNet;

internal static class SystemCommandLineFactoryMethodOperationSupport
{
    public static bool TryReadArgumentValue(MethodDef method, Instruction instruction, out MethodValue value)
    {
        if (instruction.OpCode.Code is not (
                Code.Ldarg
                or Code.Ldarg_S
                or Code.Ldarg_0
                or Code.Ldarg_1
                or Code.Ldarg_2
                or Code.Ldarg_3))
        {
            value = UnknownValue.Instance;
            return false;
        }

        if (!SystemCommandLineInstructionSupport.TryGetArgumentIndex(method, instruction, out var argumentIndex))
        {
            value = UnknownValue.Instance;
            return false;
        }

        value = argumentIndex == 0 && method.MethodSig?.HasThis == true
            ? CurrentMethodInstanceValue.Instance
            : UnknownValue.Instance;
        return true;
    }

    public static MethodValue BuildArrayValue(List<MethodValue> stack, ITypeDefOrRef? elementType)
    {
        if (!string.Equals(elementType?.FullName, "System.String", StringComparison.Ordinal))
        {
            Pop(stack);
            return UnknownValue.Instance;
        }

        return Pop(stack) is Int32Value length && length.Value >= 0
            ? new StringArrayValue(length.Value)
            : UnknownValue.Instance;
    }

    public static void ApplyArrayElementAssignment(List<MethodValue> stack)
    {
        var value = Pop(stack);
        var index = Pop(stack) as Int32Value;
        var array = Pop(stack) as StringArrayValue;
        if (array is null || index is null || value is not StringValue stringValue)
        {
            return;
        }

        if (index.Value >= 0 && index.Value < array.Values.Length)
        {
            array.Values[index.Value] = stringValue.Value;
        }
    }

    public static void ApplyFieldStore(
        List<MethodValue> stack,
        IField? field,
        IDictionary<string, MethodValue> fields)
    {
        var value = ApplyMemberIdentity(Pop(stack), field?.Name);
        var target = Pop(stack);
        if (field is null || target is not CurrentMethodInstanceValue)
        {
            return;
        }

        fields[field.FullName] = value;
    }

    public static void ApplyFieldLoad(
        List<MethodValue> stack,
        IField? field,
        IReadOnlyDictionary<string, MethodValue> fields)
    {
        var target = Pop(stack);
        if (field is null || target is not CurrentMethodInstanceValue)
        {
            stack.Add(UnknownValue.Instance);
            return;
        }

        stack.Add(fields.TryGetValue(field.FullName, out var value)
            ? ApplyMemberIdentity(value, field.Name)
            : UnknownValue.Instance);
    }

    public static void ApplyStaticFieldStore(
        List<MethodValue> stack,
        IField? field,
        IDictionary<string, MethodValue> fields)
    {
        if (field is null)
        {
            Pop(stack);
            return;
        }

        fields[field.FullName] = ApplyMemberIdentity(Pop(stack), field.Name);
    }

    public static void ApplyStaticFieldLoad(
        List<MethodValue> stack,
        IField? field,
        IReadOnlyDictionary<string, MethodValue> fields)
    {
        if (field is null)
        {
            stack.Add(UnknownValue.Instance);
            return;
        }

        stack.Add(fields.TryGetValue(field.FullName, out var value)
            ? ApplyMemberIdentity(value, field.Name)
            : UnknownValue.Instance);
    }

    public static MethodValue[] PopArguments(List<MethodValue> stack, int count)
    {
        var values = new MethodValue[count];
        for (var index = count - 1; index >= 0; index--)
        {
            values[index] = Pop(stack);
        }

        return values;
    }

    public static MethodValue Pop(List<MethodValue> stack)
    {
        if (stack.Count == 0)
        {
            return UnknownValue.Instance;
        }

        var value = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return value;
    }

    private static MethodValue ApplyMemberIdentity(MethodValue value, string? memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName))
        {
            return value;
        }

        if (value is OptionValue optionValue
            && string.IsNullOrWhiteSpace(optionValue.Definition.PropertyName))
        {
            optionValue.Definition = optionValue.Definition with { PropertyName = memberName };
        }

        return value;
    }
}
