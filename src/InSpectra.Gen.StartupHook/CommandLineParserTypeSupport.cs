using System.Reflection;

internal static class CommandLineParserTypeSupport
{
    public static bool LooksLikeHeuristicOptionMember(MemberInfo member, out Type memberType)
    {
        memberType = GetMemberType(member);
        if (string.IsNullOrWhiteSpace(member.Name)
            || member.Name.Contains("k__BackingField", StringComparison.Ordinal)
            || member.Name.StartsWith("_", StringComparison.Ordinal))
        {
            return false;
        }

        if (memberType == typeof(bool) || memberType == typeof(bool?))
        {
            return true;
        }

        return IsSimpleValueType(memberType) || IsSequenceType(memberType);
    }

    public static Type GetMemberType(MemberInfo member)
        => member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object),
        };

    public static bool IsSequenceType(Type type)
    {
        if (type == typeof(string))
        {
            return false;
        }

        return type.IsArray
            || (type.IsGenericType && type.GetGenericTypeDefinition() is { } genericTypeDefinition
                && (genericTypeDefinition == typeof(IEnumerable<>)
                    || genericTypeDefinition == typeof(ICollection<>)
                    || genericTypeDefinition == typeof(IList<>)
                    || genericTypeDefinition == typeof(IReadOnlyCollection<>)
                    || genericTypeDefinition == typeof(IReadOnlyList<>)
                    || genericTypeDefinition == typeof(List<>)
                    || genericTypeDefinition == typeof(HashSet<>)));
    }

    public static bool IsBoolType(Type type)
        => Nullable.GetUnderlyingType(type) == typeof(bool) || type == typeof(bool);

    public static List<string>? ReadAllowedValues(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
        return effectiveType.IsEnum ? Enum.GetNames(effectiveType).ToList() : null;
    }

    public static string? FormatTypeName(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
        if (effectiveType == typeof(bool))
        {
            return "Boolean";
        }

        if (effectiveType == typeof(string))
        {
            return "String";
        }

        if (effectiveType == typeof(int))
        {
            return "Int32";
        }

        if (effectiveType == typeof(long))
        {
            return "Int64";
        }

        if (effectiveType == typeof(float))
        {
            return "Float";
        }

        if (effectiveType == typeof(double))
        {
            return "Double";
        }

        if (effectiveType == typeof(decimal))
        {
            return "Decimal";
        }

        if (effectiveType == typeof(DateTime))
        {
            return "DateTime";
        }

        if (effectiveType == typeof(DateTimeOffset))
        {
            return "DateTimeOffset";
        }

        if (effectiveType == typeof(TimeSpan))
        {
            return "TimeSpan";
        }

        if (effectiveType == typeof(Guid))
        {
            return "Guid";
        }

        if (effectiveType == typeof(Uri))
        {
            return "Uri";
        }

        if (effectiveType.IsEnum)
        {
            return effectiveType.Name;
        }

        if (type.IsArray)
        {
            return $"{FormatTypeName(type.GetElementType()!)}[]";
        }

        return effectiveType.Name;
    }

    public static string TrimKnownSuffix(string name)
    {
        if (name.EndsWith("Option", StringComparison.OrdinalIgnoreCase) && name.Length > "Option".Length)
        {
            return name[..^"Option".Length];
        }

        if (name.EndsWith("Options", StringComparison.OrdinalIgnoreCase) && name.Length > "Options".Length)
        {
            return name[..^"Options".Length];
        }

        return name;
    }

    public static string ConvertToKebabCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "value";
        }

        var builder = new System.Text.StringBuilder();
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character == '_')
            {
                builder.Append('-');
                continue;
            }

            if (char.IsUpper(character)
                && index > 0
                && !char.IsUpper(value[index - 1])
                && value[index - 1] != '-')
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    public static string ConvertToArgumentName(string value)
        => ConvertToKebabCase(TrimKnownSuffix(value))
            .Replace('-', '_')
            .ToUpperInvariant();

    private static bool IsSimpleValueType(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
        if (effectiveType.IsEnum)
        {
            return true;
        }

        return effectiveType == typeof(string)
            || effectiveType == typeof(bool)
            || effectiveType == typeof(byte)
            || effectiveType == typeof(short)
            || effectiveType == typeof(int)
            || effectiveType == typeof(long)
            || effectiveType == typeof(float)
            || effectiveType == typeof(double)
            || effectiveType == typeof(decimal)
            || effectiveType == typeof(Guid)
            || effectiveType == typeof(DateTime)
            || effectiveType == typeof(DateTimeOffset)
            || effectiveType == typeof(TimeSpan)
            || effectiveType == typeof(Uri);
    }
}
