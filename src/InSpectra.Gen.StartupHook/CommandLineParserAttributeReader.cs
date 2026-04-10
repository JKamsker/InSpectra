using System.Reflection;

internal static class CommandLineParserAttributeReader
{
    public static CustomAttributeData? FindCustomAttribute(MemberInfo member, string fullName)
        => member.GetCustomAttributesData()
            .FirstOrDefault(attribute => string.Equals(attribute.AttributeType.FullName, fullName, StringComparison.Ordinal));

    public static string? GetConstructorArgumentString(CustomAttributeData? attribute, int index)
    {
        if (attribute is null || attribute.ConstructorArguments.Count <= index)
        {
            return null;
        }

        return attribute.ConstructorArguments[index].Value?.ToString();
    }

    public static int GetConstructorArgumentInt(CustomAttributeData? attribute, int index)
    {
        if (attribute is null || attribute.ConstructorArguments.Count <= index)
        {
            return 0;
        }

        return attribute.ConstructorArguments[index].Value is int intValue ? intValue : 0;
    }

    public static string? GetNamedArgumentString(CustomAttributeData? attribute, string name)
    {
        if (attribute is null)
        {
            return null;
        }

        foreach (var namedArgument in attribute.NamedArguments)
        {
            if (string.Equals(namedArgument.MemberName, name, StringComparison.Ordinal))
            {
                return namedArgument.TypedValue.Value?.ToString();
            }
        }

        return null;
    }

    public static bool GetNamedArgumentBool(CustomAttributeData? attribute, string name)
    {
        if (attribute is null)
        {
            return false;
        }

        foreach (var namedArgument in attribute.NamedArguments)
        {
            if (string.Equals(namedArgument.MemberName, name, StringComparison.Ordinal)
                && namedArgument.TypedValue.Value is bool boolValue)
            {
                return boolValue;
            }
        }

        return false;
    }

    public static (string? LongName, char? ShortName) ParseOptionNames(CustomAttributeData attribute, string memberName)
    {
        string? longName = null;
        char? shortName = null;
        foreach (var argument in attribute.ConstructorArguments)
        {
            if (argument.ArgumentType == typeof(string))
            {
                longName = argument.Value?.ToString();
            }
            else if (argument.ArgumentType == typeof(char) && argument.Value is char charValue)
            {
                shortName = charValue;
            }
        }

        return (
            longName ?? CommandLineParserTypeSupport.ConvertToKebabCase(CommandLineParserTypeSupport.TrimKnownSuffix(memberName)),
            shortName);
    }
}
