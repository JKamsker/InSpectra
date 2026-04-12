namespace InSpectra.Gen.Engine.Modes.Static.Attributes.SystemCommandLine;

using InSpectra.Gen.Engine.Contracts.Signatures;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class SystemCommandLineCommandMetadataSupport
{
    public static (string? Name, string? Description) Read(TypeDef typeDef, bool isRootCommand)
    {
        return typeDef.Methods
            .Where(static method => method.IsInstanceConstructor && method.HasBody)
            .Select(constructor => TryReadConstructorMetadata(constructor, isRootCommand, out var metadata)
                ? metadata
                : ((string? Name, string? Description)?)null)
            .OfType<(string? Name, string? Description)>()
            .OrderByDescending(Score)
            .FirstOrDefault();
    }

    private static bool TryReadConstructorMetadata(
        MethodDef constructor,
        bool isRootCommand,
        out (string? Name, string? Description) metadata)
    {
        var stack = new List<MetadataValue>();
        var locals = new Dictionary<int, MetadataValue>();

        foreach (var instruction in constructor.Body.Instructions)
        {
            if (TryReadArgumentValue(constructor, instruction, out var argumentValue))
            {
                stack.Add(argumentValue);
                continue;
            }

            switch (instruction.OpCode.Code)
            {
                case Code.Nop:
                    break;
                case Code.Ldstr:
                    stack.Add(new MetadataStringValue((string)instruction.Operand));
                    break;
                case Code.Dup:
                    if (stack.Count > 0)
                    {
                        stack.Add(stack[^1]);
                    }

                    break;
                case Code.Pop:
                    Pop(stack);
                    break;
                case Code.Stloc:
                case Code.Stloc_S:
                case Code.Stloc_0:
                case Code.Stloc_1:
                case Code.Stloc_2:
                case Code.Stloc_3:
                    if (SystemCommandLineInstructionSupport.TryGetLocalIndex(instruction, out var storeIndex))
                    {
                        locals[storeIndex] = Pop(stack);
                    }

                    break;
                case Code.Ldloc:
                case Code.Ldloc_S:
                case Code.Ldloc_0:
                case Code.Ldloc_1:
                case Code.Ldloc_2:
                case Code.Ldloc_3:
                    if (SystemCommandLineInstructionSupport.TryGetLocalIndex(instruction, out var loadIndex))
                    {
                        stack.Add(locals.TryGetValue(loadIndex, out var value) ? value : MetadataUnknownValue.Instance);
                    }

                    break;
                case Code.Call:
                case Code.Callvirt:
                    if (TryReadCommandConstructorCall(stack, instruction.Operand as IMethod, isRootCommand, out metadata))
                    {
                        return true;
                    }

                    ApplyMethodCall(stack, instruction.Operand as IMethod);
                    break;
                default:
                    stack.Clear();
                    break;
            }
        }

        metadata = (null, null);
        return false;
    }

    private static bool TryReadArgumentValue(MethodDef constructor, Instruction instruction, out MetadataValue value)
    {
        if (instruction.OpCode.Code is not (
                Code.Ldarg
                or Code.Ldarg_S
                or Code.Ldarg_0
                or Code.Ldarg_1
                or Code.Ldarg_2
                or Code.Ldarg_3))
        {
            value = MetadataUnknownValue.Instance;
            return false;
        }

        if (!SystemCommandLineInstructionSupport.TryGetArgumentIndex(constructor, instruction, out var argumentIndex))
        {
            value = MetadataUnknownValue.Instance;
            return false;
        }

        value = argumentIndex == 0 && constructor.MethodSig?.HasThis == true
            ? MetadataThisValue.Instance
            : MetadataUnknownValue.Instance;
        return true;
    }

    private static bool TryReadCommandConstructorCall(
        List<MetadataValue> stack,
        IMethod? method,
        bool isRootCommand,
        out (string? Name, string? Description) metadata)
    {
        metadata = (null, null);
        if (method?.MethodSig is null)
        {
            return false;
        }

        var argumentCount = method.MethodSig.Params.Count + (method.MethodSig.HasThis ? 1 : 0);
        var arguments = PopArguments(stack, argumentCount);
        if (!method.MethodSig.HasThis
            || !string.Equals(method.Name, ".ctor", StringComparison.Ordinal)
            || arguments.Length == 0
            || arguments[0] is not MetadataThisValue
            || !IsCommandBaseType(method.DeclaringType))
        {
            return false;
        }

        var strings = arguments
            .Skip(1)
            .OfType<MetadataStringValue>()
            .Select(static value => value.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();
        metadata = isRootCommand
            ? (null, strings.FirstOrDefault())
            : (NormalizeCommandName(strings.FirstOrDefault()), strings.Skip(1).FirstOrDefault());
        return metadata.Name is not null || metadata.Description is not null;
    }

    private static bool IsCommandBaseType(ITypeDefOrRef? type)
        => type?.FullName is "System.CommandLine.Command" or "System.CommandLine.RootCommand";

    private static void ApplyMethodCall(List<MetadataValue> stack, IMethod? method)
    {
        if (method?.MethodSig is null)
        {
            stack.Clear();
            return;
        }

        PopArguments(stack, method.MethodSig.Params.Count + (method.MethodSig.HasThis ? 1 : 0));
        if (!string.Equals(method.MethodSig.RetType.FullName, "System.Void", StringComparison.Ordinal))
        {
            stack.Add(MetadataUnknownValue.Instance);
        }
    }

    private static MetadataValue[] PopArguments(List<MetadataValue> stack, int count)
    {
        var values = new MetadataValue[count];
        for (var index = count - 1; index >= 0; index--)
        {
            values[index] = Pop(stack);
        }

        return values;
    }

    private static MetadataValue Pop(List<MetadataValue> stack)
    {
        if (stack.Count == 0)
        {
            return MetadataUnknownValue.Instance;
        }

        var value = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return value;
    }

    private static string? NormalizeCommandName(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : SignatureNormalizer.NormalizeCommandKey(value);

    private static int Score((string? Name, string? Description) metadata)
        => (string.IsNullOrWhiteSpace(metadata.Name) ? 0 : 2)
            + (string.IsNullOrWhiteSpace(metadata.Description) ? 0 : 1);

    private abstract record MetadataValue;

    private sealed record MetadataStringValue(string Value) : MetadataValue;

    private sealed record MetadataThisValue : MetadataValue
    {
        public static MetadataThisValue Instance { get; } = new();
    }

    private sealed record MetadataUnknownValue : MetadataValue
    {
        public static MetadataUnknownValue Instance { get; } = new();
    }
}
