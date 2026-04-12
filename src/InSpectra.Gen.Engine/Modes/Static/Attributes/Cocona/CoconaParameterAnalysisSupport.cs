using InSpectra.Gen.Engine.Modes.Static.Attributes;
using InSpectra.Gen.Engine.Modes.Static.Inspection;
using InSpectra.Gen.Engine.Modes.Static.Metadata;

using dnlib.DotNet;

namespace InSpectra.Gen.Engine.Modes.Static.Attributes.Cocona;

internal static class CoconaParameterAnalysisSupport
{
    private static readonly string[] FrameworkNamespacePrefixes =
    [
        "Cocona",
        "Microsoft",
        "System",
    ];

    private static readonly string[] OptionAttributeNames =
    [
        "Cocona.OptionAttribute",
        "Cocona.Lite.OptionAttribute",
    ];

    private static readonly string[] ArgumentAttributeNames =
    [
        "Cocona.ArgumentAttribute",
        "Cocona.Lite.ArgumentAttribute",
    ];

    private static readonly string[] IgnoreAttributeNames =
    [
        "Cocona.IgnoreAttribute",
        "Cocona.Lite.IgnoreAttribute",
    ];

    public static bool IsIgnored(MethodDef method)
        => StaticAnalysisAttributeSupport.FindAttribute(method.CustomAttributes, IgnoreAttributeNames) is not null;

    public static (IReadOnlyList<StaticOptionDefinition> Options, IReadOnlyList<StaticValueDefinition> Values) ReadMethodParameters(MethodDef method)
    {
        var options = new List<StaticOptionDefinition>();
        var values = new List<StaticValueDefinition>();
        var valueIndex = 0;

        foreach (var parameter in method.Parameters)
        {
            AddParameterSurface(parameter, options, values, ref valueIndex);
        }

        return (options, values);
    }

    public static bool ShouldIgnoreImplicitInfrastructureMethod(MethodDef method, bool hasExplicitCommandMetadata)
    {
        if (hasExplicitCommandMetadata || HasParameterCliMetadata(method))
        {
            return false;
        }

        if (IsFrameworkType(method.DeclaringType))
        {
            return true;
        }

        var relevantParameters = method.Parameters
            .Where(static parameter => !parameter.IsHiddenThisParameter)
            .Where(static parameter => parameter.ParamDef is not null)
            .Where(parameter => !IsCancellationToken(parameter.Type))
            .Where(parameter => !IsFromServiceParameter(parameter))
            .ToArray();
        return relevantParameters.Length > 0
            && relevantParameters.All(parameter => LooksLikeInfrastructureType(parameter.Type));
    }

    public static string ConvertToKebabCase(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "value";
        }

        var result = new System.Text.StringBuilder();
        for (var index = 0; index < name.Length; index++)
        {
            if (char.IsUpper(name[index]) && index > 0)
            {
                result.Append('-');
            }

            result.Append(char.ToLowerInvariant(name[index]));
        }

        return result.ToString();
    }

    private static void AddParameterSurface(
        Parameter parameter,
        ICollection<StaticOptionDefinition> options,
        ICollection<StaticValueDefinition> values,
        ref int valueIndex)
    {
        if (ShouldSkipParameter(parameter))
        {
            return;
        }

        if (CoconaParameterSetSupport.TryReadDefinitions(parameter, out var definitions))
        {
            foreach (var option in definitions.Options)
            {
                options.Add(option);
            }

            foreach (var value in definitions.Values)
            {
                values.Add(value with { Index = value.Index + valueIndex });
            }

            valueIndex += definitions.Values.Count;
            return;
        }

        var optionAttr = StaticAnalysisAttributeSupport.FindAttribute(parameter.ParamDef!.CustomAttributes, OptionAttributeNames);
        if (optionAttr is not null)
        {
            options.Add(ReadOptionFromParameter(parameter, optionAttr));
            return;
        }

        var argumentAttr = StaticAnalysisAttributeSupport.FindAttribute(parameter.ParamDef.CustomAttributes, ArgumentAttributeNames);
        if (argumentAttr is not null)
        {
            values.Add(ReadValueFromParameter(parameter, argumentAttr, valueIndex));
            valueIndex++;
            return;
        }

        options.Add(CreateSyntheticOption(
            parameter.Name,
            parameter.Type,
            isRequired: !StaticAnalysisTypeSupport.IsBoolType(parameter.Type) && !(parameter.ParamDef?.HasConstant ?? false)));
    }

    private static StaticOptionDefinition ReadOptionFromParameter(Parameter param, CustomAttribute attr)
        => new(
            LongName: StaticAnalysisAttributeSupport.GetConstructorArgumentString(attr, 0) ?? ConvertToKebabCase(param.Name),
            ShortName: GetShortName(attr),
            IsRequired: !StaticAnalysisTypeSupport.IsBoolType(param.Type) && !(param.ParamDef?.HasConstant ?? false),
            IsSequence: StaticAnalysisTypeSupport.IsSequenceType(param.Type),
            IsBoolLike: StaticAnalysisTypeSupport.IsBoolType(param.Type),
            ClrType: StaticAnalysisTypeSupport.GetClrTypeName(param.Type),
            Description: StaticAnalysisAttributeSupport.GetNamedArgumentString(attr, "Description"),
            DefaultValue: null,
            MetaValue: null,
            AcceptedValues: StaticAnalysisTypeSupport.GetAcceptedValues(param.Type),
            PropertyName: param.Name);

    private static StaticValueDefinition ReadValueFromParameter(Parameter param, CustomAttribute attr, int index)
        => new(
            Index: StaticAnalysisAttributeSupport.GetNamedArgumentInt(attr, "Order", index),
            Name: StaticAnalysisAttributeSupport.GetNamedArgumentString(attr, "Name") ?? param.Name,
            IsRequired: !(param.ParamDef?.HasConstant ?? false),
            IsSequence: StaticAnalysisTypeSupport.IsSequenceType(param.Type),
            ClrType: StaticAnalysisTypeSupport.GetClrTypeName(param.Type),
            Description: StaticAnalysisAttributeSupport.GetNamedArgumentString(attr, "Description"),
            DefaultValue: null,
            AcceptedValues: StaticAnalysisTypeSupport.GetAcceptedValues(param.Type));

    private static StaticOptionDefinition CreateSyntheticOption(string? name, TypeSig? type, bool isRequired)
        => new(
            LongName: ConvertToKebabCase(name),
            ShortName: null,
            IsRequired: isRequired,
            IsSequence: StaticAnalysisTypeSupport.IsSequenceType(type),
            IsBoolLike: StaticAnalysisTypeSupport.IsBoolType(type),
            ClrType: StaticAnalysisTypeSupport.GetClrTypeName(type),
            Description: null,
            DefaultValue: null,
            MetaValue: null,
            AcceptedValues: StaticAnalysisTypeSupport.GetAcceptedValues(type),
            PropertyName: name);

    private static bool ShouldSkipParameter(Parameter parameter)
        => parameter.IsHiddenThisParameter
            || parameter.ParamDef is null
            || IsCancellationToken(parameter.Type)
            || IsFromServiceParameter(parameter);

    private static bool HasParameterCliMetadata(MethodDef method)
        => method.Parameters
            .Where(static parameter => !parameter.IsHiddenThisParameter)
            .Where(static parameter => parameter.ParamDef is not null)
            .Any(parameter =>
                StaticAnalysisAttributeSupport.FindAttribute(parameter.ParamDef!.CustomAttributes, OptionAttributeNames) is not null
                || StaticAnalysisAttributeSupport.FindAttribute(parameter.ParamDef.CustomAttributes, ArgumentAttributeNames) is not null
                || CoconaParameterSetSupport.ImplementsCommandParameterSet(parameter.Type.ToTypeDefOrRef()?.ResolveTypeDef()));

    private static bool IsFromServiceParameter(Parameter parameter)
        => parameter.ParamDef is not null
            && parameter.ParamDef.CustomAttributes.Any(static attr =>
                attr.AttributeType?.FullName?.EndsWith(".FromServiceAttribute", StringComparison.Ordinal) == true
                || string.Equals(attr.AttributeType?.FullName, "FromServiceAttribute", StringComparison.Ordinal));

    internal static char? GetShortName(CustomAttribute attr)
    {
        var shortNames = StaticAnalysisAttributeSupport.GetNamedArgumentStrings(attr, "ShortNames");
        if (shortNames.Length > 0 && shortNames[0].Length == 1)
        {
            return shortNames[0][0];
        }

        if (attr.ConstructorArguments.Count > 0 && attr.ConstructorArguments[0].Value is char shortName)
        {
            return shortName;
        }

        return null;
    }

    private static bool IsFrameworkType(TypeDef? typeDef)
    {
        var typeNamespace = typeDef?.Namespace?.String;
        if (HasFrameworkPrefix(typeNamespace))
        {
            return true;
        }

        var assemblyName = typeDef?.Module?.Assembly?.Name?.String;
        return HasFrameworkPrefix(assemblyName);
    }

    private static bool HasFrameworkPrefix(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && FrameworkNamespacePrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));

    private static bool LooksLikeInfrastructureType(TypeSig? typeSig)
    {
        var fullName = StaticAnalysisTypeSupport.GetClrTypeName(typeSig);
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return false;
        }

        if (fullName.Contains("Microsoft.Extensions.", StringComparison.Ordinal)
            || fullName.Contains("Microsoft.AspNetCore.", StringComparison.Ordinal)
            || fullName.Contains("Cocona.", StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(fullName, "System.IServiceProvider", StringComparison.Ordinal)
            || string.Equals(fullName, "System.IServiceScope", StringComparison.Ordinal)
            || string.Equals(fullName, "System.IAsyncDisposable", StringComparison.Ordinal);
    }

    private static bool IsCancellationToken(TypeSig? typeSig)
        => string.Equals(typeSig?.FullName, "System.Threading.CancellationToken", StringComparison.Ordinal);
}
