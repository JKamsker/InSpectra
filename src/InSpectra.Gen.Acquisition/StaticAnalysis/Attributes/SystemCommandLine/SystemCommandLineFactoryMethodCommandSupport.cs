namespace InSpectra.Gen.Acquisition.StaticAnalysis.Attributes.SystemCommandLine;

using dnlib.DotNet;

internal static class SystemCommandLineFactoryMethodCommandSupport
{
    public static MethodValue BuildConstructedValue(List<MethodValue> stack, IMethod? method)
    {
        if (method?.MethodSig is null)
        {
            return UnknownValue.Instance;
        }

        var arguments = SystemCommandLineFactoryMethodOperationSupport.PopArguments(stack, method.MethodSig.Params.Count);
        if (TryBuildCommandValue(method.DeclaringType, arguments) is { } commandValue)
        {
            return commandValue;
        }

        var constructorArguments = ConvertArguments(arguments);
        if (SystemCommandLineTypeHierarchySupport.IsOptionType(method.DeclaringType))
        {
            var optionValue = SystemCommandLineConstructorReaderSupport.TryBuildOptionValue(method.DeclaringType.ToTypeSig(), constructorArguments);
            return optionValue is not null ? new OptionValue(optionValue.Definition) : UnknownValue.Instance;
        }

        if (SystemCommandLineTypeHierarchySupport.IsArgumentType(method.DeclaringType))
        {
            var argumentValue = SystemCommandLineConstructorReaderSupport.TryBuildArgumentValue(method.DeclaringType.ToTypeSig(), constructorArguments);
            return argumentValue is not null ? new ArgumentValue(argumentValue.Definition) : UnknownValue.Instance;
        }

        return UnknownValue.Instance;
    }

    public static MethodValue? ResolveReturnValue(IReadOnlyList<MethodValue> arguments, TypeSig returnType)
    {
        var instance = arguments.FirstOrDefault();
        return instance switch
        {
            CommandValue commandValue when IsCommandType(returnType.ToTypeDefOrRef()) => commandValue,
            OptionValue optionValue when SystemCommandLineTypeHierarchySupport.IsOptionType(returnType.ToTypeDefOrRef()) => optionValue,
            ArgumentValue argumentValue when SystemCommandLineTypeHierarchySupport.IsArgumentType(returnType.ToTypeDefOrRef()) => argumentValue,
            _ => null,
        };
    }

    public static bool IsOptionAttachMethod(IMethod method)
        => string.Equals(method.Name, "AddOption", StringComparison.Ordinal)
            || string.Equals(method.Name, "Add", StringComparison.Ordinal)
                && method.MethodSig?.Params.FirstOrDefault() is { } parameter
                && SystemCommandLineTypeHierarchySupport.IsOptionType(parameter.ToTypeDefOrRef());

    public static bool IsArgumentAttachMethod(IMethod method)
        => string.Equals(method.Name, "AddArgument", StringComparison.Ordinal)
            || string.Equals(method.Name, "Add", StringComparison.Ordinal)
                && method.MethodSig?.Params.FirstOrDefault() is { } parameter
                && SystemCommandLineTypeHierarchySupport.IsArgumentType(parameter.ToTypeDefOrRef());

    public static bool IsCommandAttachMethod(IMethod method)
        => string.Equals(method.Name, "AddCommand", StringComparison.Ordinal)
            || string.Equals(method.Name, "Add", StringComparison.Ordinal)
                && method.MethodSig?.Params.FirstOrDefault() is { } parameter
                && IsCommandType(parameter.ToTypeDefOrRef());

    public static bool IsCommandType(ITypeDefOrRef? type)
    {
        for (var current = type; current is not null;)
        {
            if (string.Equals(current.FullName, "System.CommandLine.Command", StringComparison.Ordinal)
                || string.Equals(current.FullName, "System.CommandLine.RootCommand", StringComparison.Ordinal))
            {
                return true;
            }

            current = current.ResolveTypeDef()?.BaseType;
        }

        return false;
    }

    public static bool IsRootCommandType(ITypeDefOrRef? type)
    {
        for (var current = type; current is not null;)
        {
            if (string.Equals(current.FullName, "System.CommandLine.RootCommand", StringComparison.Ordinal))
            {
                return true;
            }

            current = current.ResolveTypeDef()?.BaseType;
        }

        return false;
    }

    public static string? ResolveDescription(IReadOnlyList<MethodValue> arguments, bool isRoot)
    {
        var stringValues = arguments.OfType<StringValue>()
            .Select(static value => value.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();
        if (stringValues.Length == 0)
        {
            return null;
        }

        return isRoot
            ? stringValues[0]
            : stringValues.Skip(1).FirstOrDefault();
    }

    public static string? ResolveCommandName(ITypeDefOrRef? type, IReadOnlyList<MethodValue> arguments)
    {
        var explicitName = arguments.FirstOrDefault() switch
        {
            StringValue stringValue => stringValue.Value,
            _ => null,
        };
        if (!string.IsNullOrWhiteSpace(explicitName))
        {
            return explicitName.Trim();
        }

        var typeName = type?.Name?.ToString();
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        return SystemCommandLineAttributeReader.BuildTypeDerivedCommandName(typeName);
    }

    private static ConstructorValue[] ConvertArguments(IReadOnlyList<MethodValue> arguments)
    {
        var converted = new ConstructorValue[arguments.Count];
        for (var index = 0; index < arguments.Count; index++)
        {
            converted[index] = arguments[index] switch
            {
                StringValue stringValue => new ConstructorStringValue(stringValue.Value),
                Int32Value int32Value => new ConstructorInt32Value(int32Value.Value),
                StringArrayValue stringArrayValue => new ConstructorStringArrayValue(stringArrayValue.Values),
                _ => ConstructorUnknownValue.Instance,
            };
        }

        return converted;
    }

    private static CommandValue? TryBuildCommandValue(ITypeDefOrRef? type, IReadOnlyList<MethodValue> arguments)
    {
        if (!IsCommandType(type))
        {
            return null;
        }

        var isRoot = IsRootCommandType(type);
        var displayName = isRoot
            ? null
            : ResolveCommandName(type, arguments);
        if (!isRoot && string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        var description = ResolveDescription(arguments, isRoot);
        return new CommandValue(displayName, isRoot, description);
    }
}
