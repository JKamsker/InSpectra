namespace InSpectra.Gen.Acquisition.StaticAnalysis.Attributes.SystemCommandLine;

using InSpectra.Gen.Acquisition.StaticAnalysis.Models;

using dnlib.DotNet;

internal static class SystemCommandLineSymbolConfigurationSupport
{
    public static bool TryApplyOptionMethod(IMethod? method, IReadOnlyList<ConstructorValue> arguments)
    {
        if (method?.MethodSig?.HasThis != true
            || arguments.Count == 0
            || arguments[0] is not ConstructorOptionValue optionValue)
        {
            return false;
        }

        var updated = ApplyOptionConfiguration(optionValue.Definition, method, arguments.Cast<object?>().ToArray());
        if (updated == optionValue.Definition)
        {
            return false;
        }

        optionValue.Definition = updated;
        return true;
    }

    public static bool TryApplyArgumentMethod(IMethod? method, IReadOnlyList<ConstructorValue> arguments)
    {
        if (method?.MethodSig?.HasThis != true
            || arguments.Count == 0
            || arguments[0] is not ConstructorArgumentValue argumentValue)
        {
            return false;
        }

        var updated = ApplyArgumentConfiguration(argumentValue.Definition, method, arguments.Cast<object?>().ToArray());
        if (updated == argumentValue.Definition)
        {
            return false;
        }

        argumentValue.Definition = updated;
        return true;
    }

    public static bool TryApplyOptionMethod(IMethod? method, IReadOnlyList<MethodValue> arguments)
    {
        if (method?.MethodSig?.HasThis != true
            || arguments.Count == 0
            || arguments[0] is not OptionValue optionValue)
        {
            return false;
        }

        var updated = ApplyOptionConfiguration(optionValue.Definition, method, arguments.Cast<object?>().ToArray());
        if (updated == optionValue.Definition)
        {
            return false;
        }

        optionValue.Definition = updated;
        return true;
    }

    public static bool TryApplyArgumentMethod(IMethod? method, IReadOnlyList<MethodValue> arguments)
    {
        if (method?.MethodSig?.HasThis != true
            || arguments.Count == 0
            || arguments[0] is not ArgumentValue argumentValue)
        {
            return false;
        }

        var updated = ApplyArgumentConfiguration(argumentValue.Definition, method, arguments.Cast<object?>().ToArray());
        if (updated == argumentValue.Definition)
        {
            return false;
        }

        argumentValue.Definition = updated;
        return true;
    }

    private static StaticOptionDefinition ApplyOptionConfiguration(
        StaticOptionDefinition definition,
        IMethod method,
        IReadOnlyList<object?> arguments)
    {
        var methodName = method.Name.String;
        if (string.Equals(methodName, "set_Description", StringComparison.Ordinal))
        {
            return definition with { Description = ReadString(arguments, 1) ?? definition.Description };
        }

        if (string.Equals(methodName, "set_HelpName", StringComparison.Ordinal)
            || string.Equals(methodName, "set_ArgumentHelpName", StringComparison.Ordinal))
        {
            return definition with { MetaValue = ReadString(arguments, 1) ?? definition.MetaValue };
        }

        if (string.Equals(methodName, "set_Required", StringComparison.Ordinal)
            || string.Equals(methodName, "set_IsRequired", StringComparison.Ordinal))
        {
            return definition with { IsRequired = ReadBoolean(arguments, 1) ?? definition.IsRequired };
        }

        if (string.Equals(methodName, "SetDefaultValue", StringComparison.Ordinal))
        {
            return definition with { DefaultValue = ReadScalar(arguments, 1) ?? definition.DefaultValue };
        }

        if (string.Equals(methodName, "FromAmong", StringComparison.Ordinal))
        {
            return definition with { AcceptedValues = ReadAcceptedValues(arguments, 1, definition.AcceptedValues) };
        }

        return definition;
    }

    private static StaticValueDefinition ApplyArgumentConfiguration(
        StaticValueDefinition definition,
        IMethod method,
        IReadOnlyList<object?> arguments)
    {
        var methodName = method.Name.String;
        if (string.Equals(methodName, "set_Description", StringComparison.Ordinal))
        {
            return definition with { Description = ReadString(arguments, 1) ?? definition.Description };
        }

        if (string.Equals(methodName, "set_HelpName", StringComparison.Ordinal)
            || string.Equals(methodName, "set_ArgumentHelpName", StringComparison.Ordinal))
        {
            return definition with { Name = ReadString(arguments, 1) ?? definition.Name };
        }

        if (string.Equals(methodName, "set_Required", StringComparison.Ordinal)
            || string.Equals(methodName, "set_IsRequired", StringComparison.Ordinal))
        {
            return definition with { IsRequired = ReadBoolean(arguments, 1) ?? definition.IsRequired };
        }

        if (string.Equals(methodName, "SetDefaultValue", StringComparison.Ordinal))
        {
            return definition with { DefaultValue = ReadScalar(arguments, 1) ?? definition.DefaultValue };
        }

        if (string.Equals(methodName, "FromAmong", StringComparison.Ordinal))
        {
            return definition with { AcceptedValues = ReadAcceptedValues(arguments, 1, definition.AcceptedValues) };
        }

        return definition;
    }

    private static string? ReadString(IReadOnlyList<object?> arguments, int index)
        => index < arguments.Count
            ? arguments[index] switch
            {
                ConstructorStringValue stringValue => stringValue.Value,
                StringValue stringValue => stringValue.Value,
                _ => null,
            }
            : null;

    private static bool? ReadBoolean(IReadOnlyList<object?> arguments, int index)
    {
        if (index >= arguments.Count)
        {
            return null;
        }

        return arguments[index] switch
        {
            ConstructorInt32Value intValue => intValue.Value != 0,
            Int32Value intValue => intValue.Value != 0,
            _ => null,
        };
    }

    private static string? ReadScalar(IReadOnlyList<object?> arguments, int index)
    {
        if (index >= arguments.Count)
        {
            return null;
        }

        return arguments[index] switch
        {
            ConstructorStringValue stringValue => stringValue.Value,
            StringValue stringValue => stringValue.Value,
            ConstructorInt32Value intValue => intValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Int32Value intValue => intValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => null,
        };
    }

    private static IReadOnlyList<string> ReadAcceptedValues(
        IReadOnlyList<object?> arguments,
        int index,
        IReadOnlyList<string> fallback)
    {
        if (index >= arguments.Count)
        {
            return fallback;
        }

        var values = arguments[index] switch
        {
            ConstructorStringArrayValue arrayValue => arrayValue.Values,
            StringArrayValue arrayValue => arrayValue.Values,
            ConstructorStringValue stringValue => [stringValue.Value],
            StringValue stringValue => [stringValue.Value],
            _ => [],
        };

        var normalized = values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return normalized.Length == 0
            ? fallback
            : normalized;
    }
}
