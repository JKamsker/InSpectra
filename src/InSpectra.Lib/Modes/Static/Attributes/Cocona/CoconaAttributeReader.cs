namespace InSpectra.Lib.Modes.Static.Attributes.Cocona;

using InSpectra.Lib.Modes.Static.Inspection;
using InSpectra.Lib.Modes.Static.Metadata;

using dnlib.DotNet;

internal sealed class CoconaAttributeReader : IStaticAttributeReader
{
    private static readonly string[] CommandAttributeNames =
    [
        "Cocona.CommandAttribute",
        "Cocona.Lite.CommandAttribute",
    ];

    private static readonly string[] PrimaryCommandAttributeNames =
    [
        "Cocona.PrimaryCommandAttribute",
        "Cocona.Lite.PrimaryCommandAttribute",
    ];

    private static readonly string[] HasSubCommandsAttributeNames =
    [
        "Cocona.HasSubCommandsAttribute",
        "Cocona.Lite.HasSubCommandsAttribute",
    ];

    public IReadOnlyDictionary<string, StaticCommandDefinition> Read(IReadOnlyList<ScannedModule> modules)
    {
        var commands = new Dictionary<string, StaticCommandDefinition>(StringComparer.OrdinalIgnoreCase);
        var referencedSubcommandTypes = CollectReferencedSubcommandTypes(modules);

        foreach (var scannedModule in modules)
        {
            foreach (var typeDef in scannedModule.Module.GetTypes())
            {
                if (!IsEligibleCommandType(typeDef)
                    || referencedSubcommandTypes.Contains(typeDef.FullName))
                {
                    continue;
                }

                ReadCommandContainer(typeDef, commands, prefix: null, new HashSet<string>(StringComparer.Ordinal));
            }
        }

        return commands;
    }

    private static void ReadCommandContainer(
        TypeDef typeDef,
        IDictionary<string, StaticCommandDefinition> commands,
        string? prefix,
        ISet<string> visitedTypeNames)
    {
        if (!visitedTypeNames.Add(typeDef.FullName))
        {
            return;
        }

        try
        {
            ReadCommandMethods(typeDef, commands, prefix);
            ReadNestedSubcommands(typeDef, commands, prefix, visitedTypeNames);
        }
        finally
        {
            visitedTypeNames.Remove(typeDef.FullName);
        }
    }

    private static void ReadCommandMethods(
        TypeDef typeDef,
        IDictionary<string, StaticCommandDefinition> commands,
        string? prefix)
    {
        var candidates = typeDef.Methods
            .Where(static method => method.IsPublic && !method.IsStatic && !method.IsConstructor && !method.IsSpecialName)
            .Select(static method => CreateCommandCandidate(method))
            .Where(static candidate => candidate is not null)
            .Cast<CommandCandidate>()
            .ToArray();

        var useImplicitDefault = candidates.Length == 1
            && candidates[0].ExplicitName is null
            && !candidates[0].IsPrimary
            && !HasNestedSubcommands(typeDef);

        foreach (var candidate in candidates)
        {
            var isDefault = candidate.IsPrimary || useImplicitDefault;
            var localName = candidate.ExplicitName ?? CoconaParameterAnalysisSupport.ConvertToKebabCase(candidate.Method.Name?.String);
            var key = isDefault
                ? prefix ?? string.Empty
                : BuildQualifiedKey(prefix, localName);
            var (options, values) = CoconaParameterAnalysisSupport.ReadMethodParameters(candidate.Method);
            var definition = new StaticCommandDefinition(
                Name: string.IsNullOrEmpty(key) ? null : key,
                Description: candidate.Description,
                IsDefault: isDefault,
                IsHidden: false,
                Values: values.OrderBy(static value => value.Index).ToArray(),
                Options: options
                    .OrderByDescending(static option => option.IsRequired)
                    .ThenBy(static option => option.LongName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static option => option.ShortName)
                    .ToArray());

            StaticCommandDefinitionSupport.UpsertBest(commands, key, definition);
        }
    }

    private static void ReadNestedSubcommands(
        TypeDef typeDef,
        IDictionary<string, StaticCommandDefinition> commands,
        string? prefix,
        ISet<string> visitedTypeNames)
    {
        foreach (var attr in typeDef.CustomAttributes)
        {
            if (!HasSubCommandsAttributeNames.Contains(attr.AttributeType?.FullName, StringComparer.Ordinal))
            {
                continue;
            }

            var subcommandType = ResolveSubcommandType(typeDef, attr);
            if (subcommandType is null || !IsEligibleCommandType(subcommandType))
            {
                continue;
            }

            var localName = ResolveSubcommandName(subcommandType, attr);
            if (string.IsNullOrWhiteSpace(localName))
            {
                continue;
            }

            var key = BuildQualifiedKey(prefix, localName);
            var description = StaticAnalysisAttributeSupport.GetNamedArgumentString(attr, "Description")
                ?? StaticAnalysisAttributeSupport.GetNamedArgumentString(
                    StaticAnalysisAttributeSupport.FindAttribute(subcommandType.CustomAttributes, CommandAttributeNames),
                    "Description");
            StaticCommandDefinitionSupport.UpsertBest(
                commands,
                key,
                new StaticCommandDefinition(
                    Name: key,
                    Description: description,
                    IsDefault: false,
                    IsHidden: false,
                    Values: [],
                    Options: []));

            ReadCommandContainer(subcommandType, commands, key, visitedTypeNames);
        }
    }

    private static CommandCandidate? CreateCommandCandidate(MethodDef method)
    {
        if (CoconaParameterAnalysisSupport.IsIgnored(method))
        {
            return null;
        }

        var commandAttr = StaticAnalysisAttributeSupport.FindAttribute(method.CustomAttributes, CommandAttributeNames);
        var isPrimary = StaticAnalysisAttributeSupport.FindAttribute(method.CustomAttributes, PrimaryCommandAttributeNames) is not null;
        if (CoconaParameterAnalysisSupport.ShouldIgnoreImplicitInfrastructureMethod(method, commandAttr is not null || isPrimary))
        {
            return null;
        }

        return new CommandCandidate(
            method,
            StaticAnalysisAttributeSupport.GetConstructorArgumentString(commandAttr, 0),
            StaticAnalysisAttributeSupport.GetNamedArgumentString(commandAttr, "Description"),
            isPrimary);
    }

    private static HashSet<string> CollectReferencedSubcommandTypes(IReadOnlyList<ScannedModule> modules)
    {
        var referencedTypeNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var scannedModule in modules)
        {
            foreach (var typeDef in scannedModule.Module.GetTypes())
            {
                foreach (var attr in typeDef.CustomAttributes)
                {
                    if (!HasSubCommandsAttributeNames.Contains(attr.AttributeType?.FullName, StringComparer.Ordinal))
                    {
                        continue;
                    }

                    var subcommandType = ResolveSubcommandType(typeDef, attr);
                    if (subcommandType is not null)
                    {
                        referencedTypeNames.Add(subcommandType.FullName);
                    }
                }
            }
        }

        return referencedTypeNames;
    }

    private static TypeDef? ResolveSubcommandType(TypeDef ownerType, CustomAttribute attr)
    {
        if (attr.ConstructorArguments.Count == 0)
        {
            return null;
        }

        return attr.ConstructorArguments[0].Value switch
        {
            ClassSig classSig => classSig.TypeDefOrRef.ResolveTypeDef()
                ?? ResolveSubcommandTypeByName(ownerType, classSig.TypeDefOrRef.FullName),
            TypeSig typeSig => typeSig.ToTypeDefOrRef()?.ResolveTypeDef()
                ?? ResolveSubcommandTypeByName(ownerType, typeSig.FullName),
            ITypeDefOrRef typeDefOrRef => typeDefOrRef.ResolveTypeDef()
                ?? ResolveSubcommandTypeByName(ownerType, typeDefOrRef.FullName),
            UTF8String utf8String => ResolveSubcommandTypeByName(ownerType, utf8String.String),
            string typeName => ResolveSubcommandTypeByName(ownerType, typeName),
            _ => null,
        };
    }

    private static TypeDef? ResolveSubcommandTypeByName(TypeDef ownerType, string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        var normalizedName = typeName.Split(',')[0].Trim();
        return ownerType.Module.GetTypes().FirstOrDefault(type =>
            string.Equals(type.FullName, normalizedName, StringComparison.Ordinal)
            || string.Equals($"{type.Namespace}.{type.Name}", normalizedName, StringComparison.Ordinal)
            || string.Equals(type.Name?.String, normalizedName, StringComparison.Ordinal));
    }

    private static string? ResolveSubcommandName(TypeDef subcommandType, CustomAttribute attr)
    {
        var explicitName = StaticAnalysisAttributeSupport.GetNamedArgumentString(attr, "Name")
            ?? StaticAnalysisAttributeSupport.GetConstructorArgumentString(attr, 1)
            ?? StaticAnalysisAttributeSupport.GetConstructorArgumentString(
                StaticAnalysisAttributeSupport.FindAttribute(subcommandType.CustomAttributes, CommandAttributeNames),
                0);
        if (!string.IsNullOrWhiteSpace(explicitName))
        {
            return explicitName;
        }

        return CoconaParameterAnalysisSupport.ConvertToKebabCase(subcommandType.Name?.String);
    }

    private static bool IsEligibleCommandType(TypeDef typeDef)
        => typeDef.IsClass && !typeDef.IsAbstract && !typeDef.IsInterface;

    private static bool HasNestedSubcommands(TypeDef typeDef)
        => typeDef.CustomAttributes.Any(attr =>
            HasSubCommandsAttributeNames.Contains(attr.AttributeType?.FullName, StringComparer.Ordinal));

    private static string BuildQualifiedKey(string? prefix, string? localName)
        => string.IsNullOrWhiteSpace(prefix)
            ? localName ?? string.Empty
            : $"{prefix} {localName}".Trim();

    private sealed record CommandCandidate(
        MethodDef Method,
        string? ExplicitName,
        string? Description,
        bool IsPrimary);
}
