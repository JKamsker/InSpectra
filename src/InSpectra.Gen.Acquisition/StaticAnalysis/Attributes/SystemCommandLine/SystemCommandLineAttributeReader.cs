namespace InSpectra.Gen.Acquisition.StaticAnalysis.Attributes.SystemCommandLine;

using InSpectra.Gen.Acquisition.Help.Signatures;
using InSpectra.Gen.Acquisition.StaticAnalysis.Models;
using InSpectra.Gen.Acquisition.StaticAnalysis.Inspection;
using dnlib.DotNet;

/// <summary>
/// Basic reader for System.CommandLine tools. Since System.CommandLine is primarily
/// code-driven (new RootCommand(), new Command("name"), new Option&lt;T&gt;("--name")),
/// static attribute analysis is limited. This reader looks for classes that inherit
/// from System.CommandLine.Command/RootCommand and reads their properties decorated
/// with System.CommandLine.Binding.BinderBase or similar patterns.
///
/// Most of the CLI structure will come from the help crawl fallback.
/// </summary>
internal sealed class SystemCommandLineAttributeReader : IStaticAttributeReader
{
    private static readonly string[] CommandBaseTypeNames =
    [
        "System.CommandLine.Command",
        "System.CommandLine.RootCommand",
    ];

    public IReadOnlyDictionary<string, StaticCommandDefinition> Read(IReadOnlyList<ScannedModule> modules)
    {
        var commands = new Dictionary<string, StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var scannedModule in modules)
        {
            foreach (var typeDef in scannedModule.Module.GetTypes())
            {
                if (!typeDef.IsClass || typeDef.IsAbstract || typeDef.IsInterface)
                {
                    continue;
                }

                if (!InheritsFromCommand(typeDef))
                {
                    continue;
                }

                var definition = ReadCommandType(typeDef);
                if (definition is null)
                {
                    continue;
                }

                var key = definition.Name ?? string.Empty;
                UpsertMerged(commands, key, definition);
            }

            foreach (var pair in SystemCommandLineFactoryMethodReaderSupport.Read(scannedModule.Module))
            {
                UpsertMerged(commands, pair.Key, pair.Value);
            }
        }

        return commands;
    }

    internal static StaticCommandDefinition? ReadCommandType(TypeDef typeDef)
    {
        var isRoot = IsRootCommand(typeDef);
        var constructorMetadata = SystemCommandLineCommandMetadataSupport.Read(typeDef, isRoot);
        var name = isRoot
            ? null
            : constructorMetadata.Name ?? BuildTypeDerivedCommandName(typeDef.Name?.String);
        if (!isRoot && string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var options = new List<StaticOptionDefinition>();
        var values = new List<StaticValueDefinition>();
        foreach (var field in StaticAnalysisHierarchySupport.GetFieldsFromHierarchy(typeDef))
        {
            if (field.IsStatic || field.IsSpecialName || field.Name?.String.Contains("k__BackingField", StringComparison.Ordinal) == true)
            {
                continue;
            }

            var fieldType = field.FieldSig?.Type;
            if (fieldType is null)
            {
                continue;
            }

            if (IsOptionType(fieldType))
            {
                options.Add(BuildOptionDefinition(field.Name?.String, fieldType));
                continue;
            }

            if (IsArgumentType(fieldType))
            {
                values.Add(BuildValueDefinition(field.Name?.String, fieldType));
            }
        }

        foreach (var property in StaticAnalysisHierarchySupport.GetPropertiesFromHierarchy(typeDef))
        {
            var propertyType = property.PropertySig?.RetType;
            if (propertyType is null || property.GetMethod?.IsStatic == true || property.SetMethod?.IsStatic == true)
            {
                continue;
            }

            if (IsOptionType(propertyType))
            {
                options.Add(BuildOptionDefinition(property.Name?.String, propertyType));
                continue;
            }

            if (IsArgumentType(propertyType))
            {
                values.Add(BuildValueDefinition(property.Name?.String, propertyType));
            }
        }

        var constructorSurface = SystemCommandLineConstructorReaderSupport.ReadSurface(typeDef);
        options.AddRange(constructorSurface.Options);
        values.AddRange(constructorSurface.Values);

        return SystemCommandLineAttributeMergeSupport.CreateCommandDefinition(
            name,
            constructorMetadata.Description,
            isRoot,
            values,
            options);
    }

    private static bool InheritsFromCommand(TypeDef typeDef)
    {
        for (var current = typeDef.BaseType; current is not null;)
        {
            var fullName = current.FullName;
            if (CommandBaseTypeNames.Any(n => string.Equals(fullName, n, StringComparison.Ordinal)))
            {
                return true;
            }

            var resolved = current.ResolveTypeDef();
            if (resolved is null)
            {
                break;
            }

            current = resolved.BaseType;
        }

        return false;
    }

    private static bool IsRootCommand(TypeDef typeDef)
    {
        for (var current = typeDef.BaseType; current is not null;)
        {
            if (string.Equals(current.FullName, "System.CommandLine.RootCommand", StringComparison.Ordinal))
            {
                return true;
            }

            var resolved = current.ResolveTypeDef();
            current = resolved?.BaseType;
        }

        return false;
    }

    private static bool IsOptionType(TypeSig? typeSig)
        => SystemCommandLineTypeHierarchySupport.IsOptionType(typeSig?.ToTypeDefOrRef());

    private static bool IsArgumentType(TypeSig? typeSig)
        => SystemCommandLineTypeHierarchySupport.IsArgumentType(typeSig?.ToTypeDefOrRef());

    private static StaticOptionDefinition BuildOptionDefinition(string? memberName, TypeSig memberType)
    {
        var innerType = SystemCommandLineTypeHierarchySupport.ExtractOptionValueType(memberType);
        return new StaticOptionDefinition(
            LongName: BuildMemberDerivedOptionName(memberName),
            ShortName: null,
            IsRequired: false,
            IsSequence: StaticAnalysisTypeSupport.IsSequenceType(innerType),
            IsBoolLike: StaticAnalysisTypeSupport.IsBoolType(innerType),
            ClrType: StaticAnalysisTypeSupport.GetClrTypeName(innerType),
            Description: null,
            DefaultValue: null,
            MetaValue: null,
            AcceptedValues: StaticAnalysisTypeSupport.GetAcceptedValues(innerType),
            PropertyName: memberName);
    }

    internal static string BuildMemberDerivedOptionName(string? memberName)
        => ConvertToKebabCase(StripSuffix(NormalizeMemberName(memberName), "Option"));

    private static StaticValueDefinition BuildValueDefinition(string? memberName, TypeSig memberType)
    {
        var innerType = SystemCommandLineTypeHierarchySupport.ExtractArgumentValueType(memberType);
        var normalizedMemberName = NormalizeMemberName(memberName);
        return new StaticValueDefinition(
            Index: 0,
            Name: ConvertToKebabCase(StripSuffix(normalizedMemberName, "Argument")),
            IsRequired: true,
            IsSequence: StaticAnalysisTypeSupport.IsSequenceType(innerType),
            ClrType: StaticAnalysisTypeSupport.GetClrTypeName(innerType),
            Description: null,
            DefaultValue: null,
            AcceptedValues: StaticAnalysisTypeSupport.GetAcceptedValues(innerType));
    }

    private static string? StripSuffix(string? name, string suffix)
    {
        if (name is null) return null;
        return name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && name.Length > suffix.Length
            ? name[..^suffix.Length]
            : name;
    }

    private static string? NormalizeMemberName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return name.TrimStart('_');
    }

    private static string ConvertToKebabCase(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "value";
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0 && !char.IsUpper(name[i - 1])) sb.Append('-');
            sb.Append(char.ToLowerInvariant(name[i]));
        }

        return sb.ToString();
    }

    internal static string? BuildTypeDerivedCommandName(string? typeName)
    {
        var trimmed = StripSuffix(typeName, "Command");
        return string.IsNullOrWhiteSpace(trimmed)
            ? null
            : SignatureNormalizer.NormalizeCommandKey(ConvertToKebabCase(trimmed));
    }

    internal static void UpsertMerged(
        IDictionary<string, StaticCommandDefinition> commands,
        string key,
        StaticCommandDefinition candidate)
    {
        if (!commands.TryGetValue(key, out var existing))
        {
            commands[key] = candidate;
            return;
        }

        commands[key] = SystemCommandLineAttributeMergeSupport.Merge(existing, candidate);
    }
}
