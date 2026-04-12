using InSpectra.Gen.Engine.Modes.Static.Attributes;
using InSpectra.Gen.Engine.Modes.Static.Inspection;
using InSpectra.Gen.Engine.Modes.Static.Metadata;

using dnlib.DotNet;

namespace InSpectra.Gen.Engine.Modes.Static.Attributes.Cocona;

internal static class CoconaParameterSetSupport
{
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

    private static readonly string[] HasDefaultValueAttributeNames =
    [
        "Cocona.HasDefaultValueAttribute",
        "Cocona.Lite.HasDefaultValueAttribute",
    ];

    public static bool TryReadDefinitions(
        Parameter parameter,
        out (IReadOnlyList<StaticOptionDefinition> Options, IReadOnlyList<StaticValueDefinition> Values) definitions)
    {
        definitions = default;
        var parameterSetType = parameter.Type.ToTypeDefOrRef()?.ResolveTypeDef();
        if (parameterSetType is null || !ImplementsCommandParameterSet(parameterSetType))
        {
            return false;
        }

        definitions = ReadDefinitions(parameterSetType);
        return true;
    }

    public static bool ImplementsCommandParameterSet(TypeDef? typeDef)
    {
        if (typeDef is null)
        {
            return false;
        }

        if (typeDef.Interfaces.Any(static iface => string.Equals(iface.Interface?.FullName, "Cocona.ICommandParameterSet", StringComparison.Ordinal)
            || string.Equals(iface.Interface?.FullName, "Cocona.Lite.ICommandParameterSet", StringComparison.Ordinal)
            || string.Equals(iface.Interface?.FullName, "ICommandParameterSet", StringComparison.Ordinal)))
        {
            return true;
        }

        return typeDef.BaseType?.ResolveTypeDef() is { } baseType
            && ImplementsCommandParameterSet(baseType);
    }

    private static (IReadOnlyList<StaticOptionDefinition> Options, IReadOnlyList<StaticValueDefinition> Values) ReadDefinitions(TypeDef parameterSetType)
    {
        var constructorDefinitions = parameterSetType.Methods
            .Where(static method => method.IsConstructor && !method.IsStatic && method.IsPublic)
            .Select(CoconaParameterAnalysisSupport.ReadMethodParameters)
            .Where(static definitions => definitions.Options.Count > 0 || definitions.Values.Count > 0)
            .OrderByDescending(static definitions => definitions.Options.Count + definitions.Values.Count)
            .FirstOrDefault();
        if (constructorDefinitions.Options is not null)
        {
            return constructorDefinitions;
        }

        var options = new List<StaticOptionDefinition>();
        var values = new List<StaticValueDefinition>();
        var valueIndex = 0;

        foreach (var property in StaticAnalysisHierarchySupport.GetPropertiesFromHierarchy(parameterSetType))
        {
            if (property.GetMethod?.IsStatic == true
                || property.GetMethod is null && property.SetMethod is null
                || IsFromServiceProperty(property))
            {
                continue;
            }

            var optionAttr = StaticAnalysisAttributeSupport.FindAttribute(property.CustomAttributes, OptionAttributeNames);
            if (optionAttr is not null)
            {
                options.Add(ReadOptionFromProperty(property, optionAttr));
                continue;
            }

            var argumentAttr = StaticAnalysisAttributeSupport.FindAttribute(property.CustomAttributes, ArgumentAttributeNames);
            if (argumentAttr is not null)
            {
                values.Add(ReadValueFromProperty(property, argumentAttr, valueIndex));
                valueIndex++;
            }
        }

        return (options, values);
    }

    private static StaticOptionDefinition ReadOptionFromProperty(PropertyDef property, CustomAttribute attr)
    {
        var propertyType = property.PropertySig?.RetType;
        var hasDefaultValue = StaticAnalysisAttributeSupport.FindAttribute(property.CustomAttributes, HasDefaultValueAttributeNames) is not null;
        return new StaticOptionDefinition(
            LongName: StaticAnalysisAttributeSupport.GetConstructorArgumentString(attr, 0) ?? CoconaParameterAnalysisSupport.ConvertToKebabCase(property.Name?.String),
            ShortName: CoconaParameterAnalysisSupport.GetShortName(attr),
            IsRequired: !hasDefaultValue && !StaticAnalysisTypeSupport.IsBoolType(propertyType),
            IsSequence: StaticAnalysisTypeSupport.IsSequenceType(propertyType),
            IsBoolLike: StaticAnalysisTypeSupport.IsBoolType(propertyType),
            ClrType: StaticAnalysisTypeSupport.GetClrTypeName(propertyType),
            Description: StaticAnalysisAttributeSupport.GetNamedArgumentString(attr, "Description"),
            DefaultValue: null,
            MetaValue: null,
            AcceptedValues: StaticAnalysisTypeSupport.GetAcceptedValues(propertyType),
            PropertyName: property.Name?.String);
    }

    private static StaticValueDefinition ReadValueFromProperty(PropertyDef property, CustomAttribute attr, int fallbackIndex)
    {
        var propertyType = property.PropertySig?.RetType;
        var hasDefaultValue = StaticAnalysisAttributeSupport.FindAttribute(property.CustomAttributes, HasDefaultValueAttributeNames) is not null;
        return new StaticValueDefinition(
            Index: StaticAnalysisAttributeSupport.GetNamedArgumentInt(attr, "Order", fallbackIndex),
            Name: StaticAnalysisAttributeSupport.GetNamedArgumentString(attr, "Name") ?? property.Name?.String,
            IsRequired: !hasDefaultValue,
            IsSequence: StaticAnalysisTypeSupport.IsSequenceType(propertyType),
            ClrType: StaticAnalysisTypeSupport.GetClrTypeName(propertyType),
            Description: StaticAnalysisAttributeSupport.GetNamedArgumentString(attr, "Description"),
            DefaultValue: null,
            AcceptedValues: StaticAnalysisTypeSupport.GetAcceptedValues(propertyType));
    }

    private static bool IsFromServiceProperty(PropertyDef property)
        => property.CustomAttributes.Any(static attr =>
            attr.AttributeType?.FullName?.EndsWith(".FromServiceAttribute", StringComparison.Ordinal) == true
            || string.Equals(attr.AttributeType?.FullName, "FromServiceAttribute", StringComparison.Ordinal));
}
