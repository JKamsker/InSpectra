using System.Reflection;
using InSpectra.Gen.StartupHook.Capture;
using InSpectra.Gen.StartupHook.Reflection;

namespace InSpectra.Gen.StartupHook.CommandLineParser;

internal static class CommandLineParserTreeWalker
{
    private const string VerbAttributeName = "CommandLine.VerbAttribute";
    private const string OptionAttributeName = "CommandLine.OptionAttribute";
    private const string ValueAttributeName = "CommandLine.ValueAttribute";

    public static bool TryWalk(object parseResult, out CapturedCommand? root)
    {
        root = null;

        var typeInfo = ReflectionValueReader.GetMemberValue(parseResult, "TypeInfo");
        if (typeInfo is null)
        {
            return false;
        }

        var choiceTypes = ReflectionValueReader.GetEnumerable<Type>(typeInfo, "Choices")
            .Where(IsUsableCommandType)
            .Distinct()
            .ToArray();
        if (choiceTypes.Length > 0)
        {
            root = new CapturedCommand();
            foreach (var choiceType in choiceTypes)
            {
                var command = BuildCommand(choiceType, isRoot: false);
                if (!string.IsNullOrWhiteSpace(command.Name))
                {
                    root.Subcommands.Add(command);
                }
            }

            return root.Subcommands.Count > 0;
        }

        var currentType = ResolveCurrentType(typeInfo);
        if (!IsUsableCommandType(currentType))
        {
            return false;
        }

        root = BuildCommand(currentType!, isRoot: true);
        return root.Options.Count > 0
            || root.Arguments.Count > 0
            || root.Subcommands.Count > 0
            || !string.IsNullOrWhiteSpace(root.Description);
    }

    private static CapturedCommand BuildCommand(Type type, bool isRoot)
    {
        var verbAttribute = CommandLineParserAttributeReader.FindCustomAttribute(type, VerbAttributeName);
        var command = new CapturedCommand
        {
            Name = isRoot ? null : ResolveCommandName(type, verbAttribute),
            Description = CommandLineParserAttributeReader.GetNamedArgumentString(verbAttribute, "HelpText"),
            IsHidden = CommandLineParserAttributeReader.GetNamedArgumentBool(verbAttribute, "Hidden"),
        };

        var attributedOptions = ReadAttributedOptions(type);
        var attributedArguments = ReadAttributedArguments(type);
        if (attributedOptions.Count == 0 && attributedArguments.Count == 0)
        {
            attributedOptions = ReadHeuristicOptions(type);
        }

        command.Options.AddRange(attributedOptions);
        command.Arguments.AddRange(attributedArguments);
        EnsureBuiltInCommandOptions(command);
        return command;
    }

    private static List<CapturedOption> ReadAttributedOptions(Type type)
    {
        var options = new List<CapturedOption>();
        foreach (var member in EnumerateHierarchyMembers(type))
        {
            var optionAttribute = CommandLineParserAttributeReader.FindCustomAttribute(member, OptionAttributeName);
            if (optionAttribute is null)
            {
                continue;
            }

            options.Add(BuildAttributedOption(member, optionAttribute));
        }

        return DeduplicateOptions(options);
    }

    private static List<CapturedArgument> ReadAttributedArguments(Type type)
    {
        var values = new List<(int Index, CapturedArgument Argument)>();
        foreach (var member in EnumerateHierarchyMembers(type))
        {
            var valueAttribute = CommandLineParserAttributeReader.FindCustomAttribute(member, ValueAttributeName);
            if (valueAttribute is null)
            {
                continue;
            }

            values.Add((CommandLineParserAttributeReader.GetConstructorArgumentInt(valueAttribute, 0), BuildAttributedArgument(member, valueAttribute)));
        }

        return values
            .OrderBy(static value => value.Index)
            .Select(static value => value.Argument)
            .ToList();
    }

    private static List<CapturedOption> ReadHeuristicOptions(Type type)
    {
        var options = new List<CapturedOption>();
        foreach (var member in EnumerateHierarchyMembers(type))
        {
            if (!CommandLineParserTypeSupport.LooksLikeHeuristicOptionMember(member, out var memberType))
            {
                continue;
            }

            var isSequence = CommandLineParserTypeSupport.IsSequenceType(memberType);
            var isBoolLike = CommandLineParserTypeSupport.IsBoolType(memberType);
            options.Add(new CapturedOption
            {
                Name = $"--{CommandLineParserTypeSupport.ConvertToKebabCase(CommandLineParserTypeSupport.TrimKnownSuffix(member.Name))}",
                Description = null,
                IsRequired = false,
                IsHidden = false,
                MinArity = isBoolLike ? 0 : 0,
                MaxArity = isBoolLike ? 0 : (isSequence ? int.MaxValue : 1),
                ValueType = isBoolLike ? "Void" : CommandLineParserTypeSupport.FormatTypeName(memberType),
                ArgumentName = isBoolLike ? null : CommandLineParserTypeSupport.ConvertToArgumentName(member.Name),
            });
        }

        return DeduplicateOptions(options);
    }

    private static CapturedOption BuildAttributedOption(MemberInfo member, CustomAttributeData attribute)
    {
        var memberType = CommandLineParserTypeSupport.GetMemberType(member);
        var (longName, shortName) = CommandLineParserAttributeReader.ParseOptionNames(attribute, member.Name);
        var isSequence = CommandLineParserTypeSupport.IsSequenceType(memberType);
        var isBoolLike = CommandLineParserTypeSupport.IsBoolType(memberType);
        var captured = new CapturedOption
        {
            Name = longName is not null ? $"--{longName}" : $"-{shortName}",
            Description = CommandLineParserAttributeReader.GetNamedArgumentString(attribute, "HelpText"),
            IsRequired = CommandLineParserAttributeReader.GetNamedArgumentBool(attribute, "Required"),
            IsHidden = CommandLineParserAttributeReader.GetNamedArgumentBool(attribute, "Hidden"),
            MinArity = isBoolLike ? 0 : (CommandLineParserAttributeReader.GetNamedArgumentBool(attribute, "Required") ? 1 : 0),
            MaxArity = isBoolLike ? 0 : (isSequence ? int.MaxValue : 1),
            ValueType = isBoolLike ? "Void" : CommandLineParserTypeSupport.FormatTypeName(memberType),
            ArgumentName = CommandLineParserAttributeReader.GetNamedArgumentString(attribute, "MetaValue") ?? (isBoolLike ? null : CommandLineParserTypeSupport.ConvertToArgumentName(member.Name)),
            DefaultValue = CommandLineParserAttributeReader.GetNamedArgumentString(attribute, "Default"),
            HasDefaultValue = !string.IsNullOrWhiteSpace(CommandLineParserAttributeReader.GetNamedArgumentString(attribute, "Default")),
            AllowedValues = CommandLineParserTypeSupport.ReadAllowedValues(memberType),
        };

        if (longName is not null && shortName is not null)
        {
            captured.Aliases.Add($"-{shortName}");
        }
        else if (longName is null && shortName is not null)
        {
            captured.Aliases.Add($"--{CommandLineParserTypeSupport.ConvertToKebabCase(CommandLineParserTypeSupport.TrimKnownSuffix(member.Name))}");
        }

        return captured;
    }

    private static CapturedArgument BuildAttributedArgument(MemberInfo member, CustomAttributeData attribute)
    {
        var memberType = CommandLineParserTypeSupport.GetMemberType(member);
        var isSequence = CommandLineParserTypeSupport.IsSequenceType(memberType);
        var isRequired = CommandLineParserAttributeReader.GetNamedArgumentBool(attribute, "Required");
        return new CapturedArgument
        {
            Name = CommandLineParserAttributeReader.GetNamedArgumentString(attribute, "MetaName") ?? member.Name,
            Description = CommandLineParserAttributeReader.GetNamedArgumentString(attribute, "HelpText"),
            IsHidden = false,
            MinArity = isRequired ? 1 : 0,
            MaxArity = isSequence ? int.MaxValue : 1,
            ValueType = CommandLineParserTypeSupport.FormatTypeName(memberType),
            HasDefaultValue = !string.IsNullOrWhiteSpace(CommandLineParserAttributeReader.GetNamedArgumentString(attribute, "Default")),
            DefaultValue = CommandLineParserAttributeReader.GetNamedArgumentString(attribute, "Default"),
            AllowedValues = CommandLineParserTypeSupport.ReadAllowedValues(memberType),
        };
    }

    private static IEnumerable<MemberInfo> EnumerateHierarchyMembers(Type type)
    {
        var chain = new Stack<Type>();
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            chain.Push(current);
        }

        while (chain.Count > 0)
        {
            var current = chain.Pop();
            foreach (var property in current.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (property.GetIndexParameters().Length == 0)
                {
                    yield return property;
                }
            }

            foreach (var field in current.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!field.IsStatic)
                {
                    yield return field;
                }
            }
        }
    }

    private static void EnsureBuiltInCommandOptions(CapturedCommand command)
    {
        if (!command.Options.Any(static option => string.Equals(option.Name, "--help", StringComparison.Ordinal)))
        {
            command.Options.Add(new CapturedOption
            {
                Name = "--help",
                Description = "Display this help screen.",
                Aliases = ["-h"],
                MinArity = 0,
                MaxArity = 0,
                ValueType = "Void",
            });
        }

        if (!command.Options.Any(static option => string.Equals(option.Name, "--version", StringComparison.Ordinal)))
        {
            command.Options.Add(new CapturedOption
            {
                Name = "--version",
                Description = "Display version information.",
                MinArity = 0,
                MaxArity = 0,
                ValueType = "Void",
            });
        }
    }

    private static Type? ResolveCurrentType(object typeInfo)
    {
        var current = ReflectionValueReader.GetMemberValue(typeInfo, "Current");
        if (current is Type currentType)
        {
            return currentType;
        }

        if (current is null || string.Equals(current.GetType().FullName, "CommandLine.NullInstance", StringComparison.Ordinal))
        {
            return null;
        }

        return current.GetType();
    }

    private static bool IsUsableCommandType(Type? type)
        => type is not null
            && type != typeof(object)
            && !string.Equals(type.FullName, "CommandLine.NullInstance", StringComparison.Ordinal);

    private static string? ResolveCommandName(Type type, CustomAttributeData? verbAttribute)
        => CommandLineParserAttributeReader.GetConstructorArgumentString(verbAttribute, 0)
            ?? CommandLineParserTypeSupport.ConvertToKebabCase(type.Name.Replace("Options", string.Empty, StringComparison.OrdinalIgnoreCase));

    private static List<CapturedOption> DeduplicateOptions(IEnumerable<CapturedOption> options)
        => options
            .GroupBy(static option => option.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .ToList();
}
