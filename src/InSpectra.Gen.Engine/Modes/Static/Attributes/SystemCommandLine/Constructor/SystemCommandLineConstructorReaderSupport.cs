namespace InSpectra.Gen.Engine.Modes.Static.Attributes.SystemCommandLine.Constructor;

using InSpectra.Gen.Engine.Modes.Static.Inspection;
using InSpectra.Gen.Engine.Modes.Static.Metadata;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class SystemCommandLineConstructorReaderSupport
{
    public static (IReadOnlyList<StaticOptionDefinition> Options, IReadOnlyList<StaticValueDefinition> Values) ReadSurface(TypeDef typeDef)
    {
        var options = new Dictionary<string, ConstructorOptionValue>(StringComparer.OrdinalIgnoreCase);
        var values = new List<ConstructorArgumentValue>();

        foreach (var constructor in typeDef.Methods.Where(static method => method.IsInstanceConstructor && method.HasBody))
        {
            ReadConstructorSurface(constructor, options, values);
        }

        return (
            options.Values
                .Select(static option => option.Definition)
                .OrderBy(static option => option.LongName ?? option.ShortName?.ToString())
                .ToArray(),
            values
                .Select(static (value, index) => value.Definition with { Index = index })
                .ToArray());
    }

    internal static ConstructorArgumentValue? TryBuildArgumentValue(TypeSig? typeSig, IReadOnlyList<ConstructorValue> arguments)
    {
        var rawName = arguments.FirstOrDefault() switch
        {
            ConstructorStringValue stringValue => stringValue.Value,
            ConstructorStringArrayValue arrayValue => arrayValue.Values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)),
            _ => null,
        };
        var name = SystemCommandLineConstructorOperationSupport.NormalizeArgumentName(rawName);
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var valueType = SystemCommandLineTypeHierarchySupport.ExtractArgumentValueType(typeSig);
        return new ConstructorArgumentValue(
            new StaticValueDefinition(
                Index: 0,
                Name: name,
                IsRequired: true,
                IsSequence: StaticAnalysisTypeSupport.IsSequenceType(valueType),
                ClrType: StaticAnalysisTypeSupport.GetClrTypeName(valueType),
                Description: arguments.Skip(1).OfType<ConstructorStringValue>().Select(static value => value.Value).FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)),
                DefaultValue: null,
                AcceptedValues: StaticAnalysisTypeSupport.GetAcceptedValues(valueType)));
    }

    internal static ConstructorOptionValue? TryBuildOptionValue(TypeSig? typeSig, IReadOnlyList<ConstructorValue> arguments)
    {
        var (longName, shortName) = SystemCommandLineConstructorOperationSupport.ReadOptionAliases(arguments.FirstOrDefault());
        if (longName is null && shortName is null)
        {
            return null;
        }

        var valueType = SystemCommandLineTypeHierarchySupport.ExtractOptionValueType(typeSig);
        return new ConstructorOptionValue(
            new StaticOptionDefinition(
                LongName: longName,
                ShortName: shortName,
                IsRequired: false,
                IsSequence: StaticAnalysisTypeSupport.IsSequenceType(valueType),
                IsBoolLike: StaticAnalysisTypeSupport.IsBoolType(valueType),
                ClrType: StaticAnalysisTypeSupport.GetClrTypeName(valueType),
                Description: arguments.Skip(1).OfType<ConstructorStringValue>().Select(static value => value.Value).FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)),
                DefaultValue: null,
                MetaValue: null,
                AcceptedValues: StaticAnalysisTypeSupport.GetAcceptedValues(valueType),
                PropertyName: null));
    }

    private static void ReadConstructorSurface(
        MethodDef constructor,
        IDictionary<string, ConstructorOptionValue> options,
        ICollection<ConstructorArgumentValue> values)
    {
        var stack = new List<ConstructorValue>();
        var locals = new Dictionary<int, ConstructorValue>();
        var instanceFields = new Dictionary<string, ConstructorValue>(StringComparer.Ordinal);
        var staticFields = new Dictionary<string, ConstructorValue>(StringComparer.Ordinal);

        foreach (var instruction in constructor.Body.Instructions)
        {
            if (SystemCommandLineInstructionSupport.TryReadInt32(instruction, out var intValue))
            {
                stack.Add(new ConstructorInt32Value(intValue));
                continue;
            }

            if (SystemCommandLineConstructorOperationSupport.TryReadArgumentValue(constructor, instruction, out var argumentValue))
            {
                stack.Add(argumentValue);
                continue;
            }

            switch (instruction.OpCode.Code)
            {
                case Code.Nop:
                case Code.Ret:
                    break;
                case Code.Ldstr:
                    stack.Add(new ConstructorStringValue((string)instruction.Operand));
                    break;
                case Code.Ldnull:
                    stack.Add(ConstructorNullValue.Instance);
                    break;
                case Code.Newarr:
                    stack.Add(SystemCommandLineConstructorOperationSupport.BuildArrayValue(stack, instruction.Operand as ITypeDefOrRef));
                    break;
                case Code.Dup:
                    if (stack.Count > 0)
                    {
                        stack.Add(stack[^1]);
                    }

                    break;
                case Code.Pop:
                    SystemCommandLineConstructorOperationSupport.Pop(stack);
                    break;
                case Code.Stelem_Ref:
                    SystemCommandLineConstructorOperationSupport.ApplyArrayElementAssignment(stack);
                    break;
                case Code.Stloc:
                case Code.Stloc_S:
                case Code.Stloc_0:
                case Code.Stloc_1:
                case Code.Stloc_2:
                case Code.Stloc_3:
                    if (SystemCommandLineInstructionSupport.TryGetLocalIndex(instruction, out var storeIndex))
                    {
                        locals[storeIndex] = SystemCommandLineConstructorOperationSupport.Pop(stack);
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
                        stack.Add(locals.TryGetValue(loadIndex, out var value) ? value : ConstructorUnknownValue.Instance);
                    }

                    break;
                case Code.Stfld:
                    SystemCommandLineConstructorOperationSupport.ApplyFieldStore(stack, instruction.Operand as IField, instanceFields);
                    break;
                case Code.Ldfld:
                    SystemCommandLineConstructorOperationSupport.ApplyFieldLoad(stack, instruction.Operand as IField, instanceFields);
                    break;
                case Code.Stsfld:
                    SystemCommandLineConstructorOperationSupport.ApplyStaticFieldStore(stack, instruction.Operand as IField, staticFields);
                    break;
                case Code.Ldsfld:
                    SystemCommandLineConstructorOperationSupport.ApplyStaticFieldLoad(stack, instruction.Operand as IField, staticFields);
                    break;
                case Code.Newobj:
                    stack.Add(BuildConstructedValue(stack, instruction.Operand as IMethod));
                    break;
                case Code.Call:
                case Code.Callvirt:
                    ApplyMethodCall(stack, instruction.Operand as IMethod, options, values);
                    break;
                default:
                    stack.Clear();
                    break;
            }
        }
    }

    private static ConstructorValue BuildConstructedValue(List<ConstructorValue> stack, IMethod? method)
    {
        if (method?.MethodSig is null)
        {
            return ConstructorUnknownValue.Instance;
        }

        var arguments = SystemCommandLineConstructorOperationSupport.PopArguments(stack, method.MethodSig.Params.Count);
        if (SystemCommandLineTypeHierarchySupport.IsOptionType(method.DeclaringType))
        {
            if (TryBuildOptionValue(method.DeclaringType.ToTypeSig(), arguments) is { } optionValue)
            {
                return optionValue;
            }

            return ConstructorUnknownValue.Instance;
        }

        if (SystemCommandLineTypeHierarchySupport.IsArgumentType(method.DeclaringType))
        {
            if (TryBuildArgumentValue(method.DeclaringType.ToTypeSig(), arguments) is { } argumentValue)
            {
                return argumentValue;
            }

            return ConstructorUnknownValue.Instance;
        }

        return ConstructorUnknownValue.Instance;
    }

    private static void ApplyMethodCall(
        List<ConstructorValue> stack,
        IMethod? method,
        IDictionary<string, ConstructorOptionValue> options,
        ICollection<ConstructorArgumentValue> values)
    {
        if (method?.MethodSig is null)
        {
            stack.Clear();
            return;
        }

        var argumentCount = method.MethodSig.Params.Count + (method.MethodSig.HasThis ? 1 : 0);
        var arguments = SystemCommandLineConstructorOperationSupport.PopArguments(stack, argumentCount);
        if (SystemCommandLineSymbolConfigurationSupport.TryApplyOptionMethod(method, arguments)
            || SystemCommandLineSymbolConfigurationSupport.TryApplyArgumentMethod(method, arguments))
        {
            if (!string.Equals(method.MethodSig.RetType.FullName, "System.Void", StringComparison.Ordinal))
            {
                stack.Add(ResolveReturnValue(arguments, method.MethodSig.RetType));
            }

            return;
        }

        if (method.MethodSig.HasThis
            && arguments.Length >= 2
            && arguments[0] is CurrentCommandValue
            && arguments[1] is ConstructorOptionValue optionValue
            && SystemCommandLineConstructorOperationSupport.IsOptionAttachMethod(method))
        {
            SystemCommandLineConstructorOperationSupport.UpsertOption(options, optionValue);
            return;
        }

        if (method.MethodSig.HasThis
            && arguments.Length >= 2
            && arguments[0] is CurrentCommandValue
            && arguments[1] is ConstructorArgumentValue argumentValue
            && SystemCommandLineConstructorOperationSupport.IsArgumentAttachMethod(method))
        {
            values.Add(argumentValue);
            return;
        }

        if (!string.Equals(method.MethodSig.RetType.FullName, "System.Void", StringComparison.Ordinal))
        {
            stack.Add(ResolveReturnValue(arguments, method.MethodSig.RetType));
        }
    }

    private static ConstructorValue ResolveReturnValue(IReadOnlyList<ConstructorValue> arguments, TypeSig returnType)
    {
        var instance = arguments.FirstOrDefault();
        return instance switch
        {
            ConstructorOptionValue optionValue when SystemCommandLineTypeHierarchySupport.IsOptionType(returnType.ToTypeDefOrRef()) => optionValue,
            ConstructorArgumentValue argumentValue when SystemCommandLineTypeHierarchySupport.IsArgumentType(returnType.ToTypeDefOrRef()) => argumentValue,
            _ => ConstructorUnknownValue.Instance,
        };
    }

}
