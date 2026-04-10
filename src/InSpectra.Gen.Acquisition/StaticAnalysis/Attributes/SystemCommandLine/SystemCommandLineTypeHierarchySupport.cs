namespace InSpectra.Gen.Acquisition.StaticAnalysis.Attributes.SystemCommandLine;

using dnlib.DotNet;

internal static class SystemCommandLineTypeHierarchySupport
{
    public static TypeSig? ExtractArgumentValueType(TypeSig? typeSig)
        => TryExtractGenericArgument(typeSig, "System.CommandLine.Argument", out var valueType)
            ? valueType
            : null;

    public static TypeSig? ExtractOptionValueType(TypeSig? typeSig)
        => TryExtractGenericArgument(typeSig, "System.CommandLine.Option", out var valueType)
            ? valueType
            : null;

    public static bool IsArgumentType(ITypeDefOrRef? type)
        => InheritsFrom(type, "System.CommandLine.Argument");

    public static bool IsOptionType(ITypeDefOrRef? type)
        => InheritsFrom(type, "System.CommandLine.Option");

    private static bool InheritsFrom(ITypeDefOrRef? type, string baseTypeName)
    {
        for (var current = type; current is not null;)
        {
            if (MatchesBaseType(current, baseTypeName))
            {
                return true;
            }

            current = current.ResolveTypeDef()?.BaseType;
        }

        return false;
    }

    private static bool MatchesBaseType(ITypeDefOrRef? type, string baseTypeName)
    {
        var fullTypeName = GetFullTypeName(type);
        return string.Equals(fullTypeName, baseTypeName, StringComparison.Ordinal)
            || fullTypeName.StartsWith(baseTypeName + "`", StringComparison.Ordinal);
    }

    private static bool TryExtractGenericArgument(TypeSig? typeSig, string baseTypeName, out TypeSig? valueType)
    {
        valueType = null;
        if (typeSig is GenericInstSig genericInstSig
            && MatchesBaseType(genericInstSig.GenericType?.ToTypeDefOrRef(), baseTypeName)
            && genericInstSig.GenericArguments.Count > 0)
        {
            valueType = genericInstSig.GenericArguments[0];
            return true;
        }

        return TryExtractGenericArgument(typeSig?.ToTypeDefOrRef(), baseTypeName, out valueType);
    }

    private static bool TryExtractGenericArgument(ITypeDefOrRef? type, string baseTypeName, out TypeSig? valueType)
    {
        for (var current = type; current is not null;)
        {
            if (current is TypeSpec typeSpec
                && typeSpec.TypeSig is GenericInstSig genericInstSig
                && MatchesBaseType(genericInstSig.GenericType?.ToTypeDefOrRef(), baseTypeName)
                && genericInstSig.GenericArguments.Count > 0)
            {
                valueType = genericInstSig.GenericArguments[0];
                return true;
            }

            current = current.ResolveTypeDef()?.BaseType;
        }

        valueType = null;
        return false;
    }

    private static string GetFullTypeName(ITypeDefOrRef? type)
        => type switch
        {
            TypeSpec typeSpec => typeSpec.TypeSig?.FullName ?? typeSpec.FullName,
            _ => type?.FullName ?? string.Empty,
        };
}
