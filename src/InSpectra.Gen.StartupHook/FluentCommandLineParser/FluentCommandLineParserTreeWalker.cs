using InSpectra.Gen.StartupHook.Capture;
using InSpectra.Gen.StartupHook.Reflection;

namespace InSpectra.Gen.StartupHook.FluentCommandLineParser;

internal static class FluentCommandLineParserTreeWalker
{
    /// <summary>
    /// Walks the parser instance by reading its <c>Options</c> property
    /// (<c>IEnumerable&lt;ICommandLineOption&gt;</c>) and mapping each option
    /// to a <see cref="CapturedOption"/>. FluentCommandLineParser has no
    /// subcommand concept, so the result is always a flat command with options.
    /// </summary>
    public static bool TryWalk(object parserInstance, out CapturedCommand? root)
    {
        root = null;

        var options = ReflectionValueReader.GetEnumerable<object>(parserInstance, "Options").ToArray();
        if (options.Length == 0)
        {
            return false;
        }

        root = new CapturedCommand();

        foreach (var option in options)
        {
            var captured = BuildOption(option);
            if (captured is not null)
            {
                root.Options.Add(captured);
            }
        }

        EnsureBuiltInOptions(root);

        return root.Options.Count > 0;
    }

    private static CapturedOption? BuildOption(object option)
    {
        var shortName = ReflectionValueReader.GetMemberValue<string>(option, "ShortName");
        var longName = ReflectionValueReader.GetMemberValue<string>(option, "LongName");

        if (string.IsNullOrWhiteSpace(shortName) && string.IsNullOrWhiteSpace(longName))
        {
            return null;
        }

        var description = ReflectionValueReader.GetMemberValue<string>(option, "Description");
        var hasDefault = ReflectionValueReader.GetMemberValue<bool>(option, "HasDefault");
        var defaultValue = ReflectionValueReader.GetMemberValue(option, "Default");
        var setupType = ReflectionValueReader.GetMemberValue<Type>(option, "SetupType");

        var isBoolType = setupType == typeof(bool) || setupType == typeof(bool?);

        var captured = new CapturedOption
        {
            Name = !string.IsNullOrWhiteSpace(longName) ? $"--{longName}" : $"-{shortName}",
            Description = description,
            IsRequired = false,
            IsHidden = false,
            MinArity = isBoolType ? 0 : 0,
            MaxArity = isBoolType ? 0 : 1,
            ValueType = isBoolType ? "Void" : FormatTypeName(setupType),
            HasDefaultValue = hasDefault,
            DefaultValue = hasDefault && defaultValue is not null ? defaultValue.ToString() : null,
        };

        if (!string.IsNullOrWhiteSpace(longName) && !string.IsNullOrWhiteSpace(shortName))
        {
            captured.Aliases.Add($"-{shortName}");
        }
        else if (string.IsNullOrWhiteSpace(longName) && !string.IsNullOrWhiteSpace(shortName))
        {
            captured.Aliases.Add($"--{shortName}");
        }

        return captured;
    }

    private static void EnsureBuiltInOptions(CapturedCommand command)
    {
        if (!command.Options.Any(static o => string.Equals(o.Name, "--help", StringComparison.Ordinal)))
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
    }

    private static string FormatTypeName(Type? type)
    {
        if (type is null)
        {
            return "String";
        }

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            type = underlying;
        }

        if (type == typeof(string)) return "String";
        if (type == typeof(int)) return "Int32";
        if (type == typeof(long)) return "Int64";
        if (type == typeof(double)) return "Double";
        if (type == typeof(float)) return "Single";
        if (type == typeof(decimal)) return "Decimal";
        if (type == typeof(bool)) return "Void";
        if (type == typeof(DateTime)) return "DateTime";
        if (type == typeof(Uri)) return "Uri";

        if (type.IsEnum)
        {
            return type.Name;
        }

        return type.Name;
    }
}
