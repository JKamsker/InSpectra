namespace InSpectra.Lib.Modes.Static.Attributes.SystemCommandLine.FactoryMethod;

using InSpectra.Lib.Modes.Static.Inspection;
using InSpectra.Lib.Modes.Static.Metadata;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class SystemCommandLineFactoryMethodReaderSupport
{
    public static IReadOnlyDictionary<string, StaticCommandDefinition> Read(ModuleDef module)
    {
        var commands = new Dictionary<string, StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var typeDef in module.GetTypes())
        {
            foreach (var method in typeDef.Methods.Where(CanBuildCommandSurface))
            {
                ReadMethodSurface(method, commands);
            }
        }

        return commands;
    }

    private static bool CanBuildCommandSurface(MethodDef method)
        => method.HasBody
            && !method.IsConstructor
            && !method.IsSpecialName
            && SystemCommandLineFactoryMethodCommandSupport.IsCommandType(method.MethodSig?.RetType?.ToTypeDefOrRef());

    private static void ReadMethodSurface(
        MethodDef method,
        IDictionary<string, StaticCommandDefinition> commands)
    {
        var stack = new List<MethodValue>();
        var locals = new Dictionary<int, MethodValue>();
        var instanceFields = new Dictionary<string, MethodValue>(StringComparer.Ordinal);
        var staticFields = new Dictionary<string, MethodValue>(StringComparer.Ordinal);
        var returnedCommands = new HashSet<CommandValue>();

        foreach (var instruction in method.Body.Instructions)
        {
            if (SystemCommandLineInstructionSupport.TryReadInt32(instruction, out var intValue))
            {
                stack.Add(new Int32Value(intValue));
                continue;
            }

            if (SystemCommandLineFactoryMethodOperationSupport.TryReadArgumentValue(method, instruction, out var argumentValue))
            {
                stack.Add(argumentValue);
                continue;
            }

            switch (instruction.OpCode.Code)
            {
                case Code.Nop:
                    break;
                case Code.Ldstr:
                    stack.Add(new StringValue((string)instruction.Operand));
                    break;
                case Code.Ldnull:
                    stack.Add(NullValue.Instance);
                    break;
                case Code.Newarr:
                    stack.Add(SystemCommandLineFactoryMethodOperationSupport.BuildArrayValue(stack, instruction.Operand as ITypeDefOrRef));
                    break;
                case Code.Dup:
                    if (stack.Count > 0)
                    {
                        stack.Add(stack[^1]);
                    }

                    break;
                case Code.Pop:
                    SystemCommandLineFactoryMethodOperationSupport.Pop(stack);
                    break;
                case Code.Stelem_Ref:
                    SystemCommandLineFactoryMethodOperationSupport.ApplyArrayElementAssignment(stack);
                    break;
                case Code.Stloc:
                case Code.Stloc_S:
                case Code.Stloc_0:
                case Code.Stloc_1:
                case Code.Stloc_2:
                case Code.Stloc_3:
                    if (SystemCommandLineInstructionSupport.TryGetLocalIndex(instruction, out var storeIndex))
                    {
                        locals[storeIndex] = SystemCommandLineFactoryMethodOperationSupport.Pop(stack);
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
                        stack.Add(locals.TryGetValue(loadIndex, out var value) ? value : UnknownValue.Instance);
                    }

                    break;
                case Code.Stfld:
                    SystemCommandLineFactoryMethodOperationSupport.ApplyFieldStore(stack, instruction.Operand as IField, instanceFields);
                    break;
                case Code.Ldfld:
                    SystemCommandLineFactoryMethodOperationSupport.ApplyFieldLoad(stack, instruction.Operand as IField, instanceFields);
                    break;
                case Code.Stsfld:
                    SystemCommandLineFactoryMethodOperationSupport.ApplyStaticFieldStore(stack, instruction.Operand as IField, staticFields);
                    break;
                case Code.Ldsfld:
                    SystemCommandLineFactoryMethodOperationSupport.ApplyStaticFieldLoad(stack, instruction.Operand as IField, staticFields);
                    break;
                case Code.Newobj:
                    stack.Add(SystemCommandLineFactoryMethodCommandSupport.BuildConstructedValue(stack, instruction.Operand as IMethod));
                    break;
                case Code.Call:
                case Code.Callvirt:
                    ApplyMethodCall(stack, instruction.Operand as IMethod);
                    break;
                case Code.Ret:
                    if (stack.Count > 0 && stack[^1] is CommandValue returnedCommand)
                    {
                        returnedCommands.Add(returnedCommand);
                    }

                    break;
                default:
                    stack.Clear();
                    break;
            }
        }

        foreach (var returnedCommand in returnedCommands)
        {
            RegisterTree(commands, returnedCommand);
        }
    }

    private static void ApplyMethodCall(
        List<MethodValue> stack,
        IMethod? method)
    {
        if (method?.MethodSig is null)
        {
            stack.Clear();
            return;
        }

        var argumentCount = method.MethodSig.Params.Count + (method.MethodSig.HasThis ? 1 : 0);
        var arguments = SystemCommandLineFactoryMethodOperationSupport.PopArguments(stack, argumentCount);
        if (SystemCommandLineSymbolConfigurationSupport.TryApplyOptionMethod(method, arguments)
            || SystemCommandLineSymbolConfigurationSupport.TryApplyArgumentMethod(method, arguments))
        {
            if (!string.Equals(method.MethodSig.RetType.FullName, "System.Void", StringComparison.Ordinal)
                && SystemCommandLineFactoryMethodCommandSupport.ResolveReturnValue(arguments, method.MethodSig.RetType) is { } configuredReturnValue)
            {
                stack.Add(configuredReturnValue);
            }

            return;
        }

        if (method.MethodSig.HasThis
            && arguments.Length >= 2
            && arguments[0] is CommandValue commandValue
            && arguments[1] is OptionValue optionValue
            && SystemCommandLineFactoryMethodCommandSupport.IsOptionAttachMethod(method))
        {
            commandValue.UpsertOption(optionValue);
            return;
        }

        if (method.MethodSig.HasThis
            && arguments.Length >= 2
            && arguments[0] is CommandValue commandValueWithArgument
            && arguments[1] is ArgumentValue argumentValue
            && SystemCommandLineFactoryMethodCommandSupport.IsArgumentAttachMethod(method))
        {
            commandValueWithArgument.AddValue(argumentValue);
            return;
        }

        if (method.MethodSig.HasThis
            && arguments.Length >= 2
            && arguments[0] is CommandValue parentCommand
            && arguments[1] is CommandValue childCommand
            && SystemCommandLineFactoryMethodCommandSupport.IsCommandAttachMethod(method))
        {
            childCommand.AttachTo(parentCommand);
            return;
        }

        if (!string.Equals(method.MethodSig.RetType.FullName, "System.Void", StringComparison.Ordinal)
            && SystemCommandLineFactoryMethodCommandSupport.ResolveReturnValue(arguments, method.MethodSig.RetType) is { } returnValue)
        {
            stack.Add(returnValue);
        }
    }

    private static void RegisterTree(IDictionary<string, StaticCommandDefinition> commands, CommandValue root)
    {
        foreach (var command in root.EnumerateSelfAndDescendants())
        {
            var key = command.FullKey ?? string.Empty;
            StaticCommandDefinitionSupport.UpsertBest(commands, key, command.ToDefinition());
        }
    }

}
