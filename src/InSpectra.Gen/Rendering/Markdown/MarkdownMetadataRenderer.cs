using System.Text;
using System.Text.Json;
using InSpectra.Gen.Models;

namespace InSpectra.Gen.Rendering.Markdown;

public sealed class MarkdownMetadataRenderer
{
    public void AppendRootMetadata(NormalizedCliDocument document, StringBuilder builder)
    {
        if (!HasMetadata(document))
        {
            return;
        }

        builder.AppendLine("## Metadata Appendix");
        builder.AppendLine();
        AppendMetadataSection("### Root", document.Source.Metadata, builder);
        AppendArgumentMetadata("### Root Arguments", document.RootArguments, builder);
        AppendOptionMetadata("### Root Options", document.RootOptions, builder);
    }

    public void AppendCommandMetadata(NormalizedCommand command, StringBuilder builder, int headingLevel)
    {
        if (!HasMetadata(command))
        {
            return;
        }

        builder.AppendLine($"{new string('#', headingLevel)} Metadata Appendix");
        builder.AppendLine();
        AppendMetadataSection("#### Command", command.Command.Metadata, builder);
        AppendArgumentMetadata(command.Arguments, builder);
        AppendOptionMetadata("#### Declared Options", command.DeclaredOptions, builder);
        AppendInheritedOptionMetadata(command.InheritedOptions, builder);
    }

    private static bool HasMetadata(NormalizedCommand command)
    {
        return command.Command.Metadata.Count > 0 ||
               command.Arguments.Any(argument => argument.Metadata.Count > 0) ||
               command.DeclaredOptions.Any(HasMetadata) ||
               command.InheritedOptions.Any(option => HasMetadata(option.Option));
    }

    private static bool HasMetadata(NormalizedCliDocument document)
    {
        return document.Source.Metadata.Count > 0 ||
               document.RootArguments.Any(argument => argument.Metadata.Count > 0) ||
               document.RootOptions.Any(HasMetadata);
    }

    private static void AppendMetadataSection(string heading, IEnumerable<OpenCliMetadata> metadata, StringBuilder builder)
    {
        var items = metadata.ToList();
        if (items.Count == 0)
        {
            return;
        }

        builder.AppendLine(heading);
        builder.AppendLine();
        AppendMetadataList(items, builder);
    }

    private static void AppendArgumentMetadata(IEnumerable<OpenCliArgument> arguments, StringBuilder builder)
    {
        AppendArgumentMetadata("#### Arguments", arguments, builder);
    }

    private static void AppendArgumentMetadata(string heading, IEnumerable<OpenCliArgument> arguments, StringBuilder builder)
    {
        var argumentMetadata = arguments.Where(argument => argument.Metadata.Count > 0).ToList();
        if (argumentMetadata.Count == 0)
        {
            return;
        }

        builder.AppendLine(heading);
        builder.AppendLine();
        foreach (var argument in argumentMetadata)
        {
            builder.AppendLine($"- `{argument.Name}`");
            AppendMetadataList(argument.Metadata, builder, "  ");
        }

        builder.AppendLine();
    }

    private static void AppendOptionMetadata(string heading, IEnumerable<OpenCliOption> options, StringBuilder builder)
    {
        var optionMetadata = options.Where(HasMetadata).ToList();
        if (optionMetadata.Count == 0)
        {
            return;
        }

        builder.AppendLine(heading);
        builder.AppendLine();
        foreach (var option in optionMetadata)
        {
            AppendOptionMetadata($"`{option.Name}`", option, builder);
        }

        builder.AppendLine();
    }

    private static void AppendInheritedOptionMetadata(IEnumerable<ResolvedOption> options, StringBuilder builder)
    {
        var inheritedMetadata = options.Where(option => HasMetadata(option.Option)).ToList();
        if (inheritedMetadata.Count == 0)
        {
            return;
        }

        builder.AppendLine("#### Inherited Options");
        builder.AppendLine();
        foreach (var option in inheritedMetadata)
        {
            AppendOptionMetadata($"`{option.Option.Name}` from `{option.InheritedFromPath}`", option.Option, builder);
        }

        builder.AppendLine();
    }

    private static bool HasMetadata(OpenCliOption option)
    {
        return option.Metadata.Count > 0 || option.Arguments.Any(argument => argument.Metadata.Count > 0);
    }

    private static void AppendOptionMetadata(string displayLabel, OpenCliOption option, StringBuilder builder)
    {
        builder.AppendLine($"- {displayLabel}");
        if (option.Metadata.Count > 0)
        {
            AppendMetadataList(option.Metadata, builder, "  ");
        }

        AppendOptionArgumentMetadata(option.Arguments, builder, "  ");
    }

    private static void AppendOptionArgumentMetadata(IEnumerable<OpenCliArgument> arguments, StringBuilder builder, string indent)
    {
        foreach (var argument in arguments.Where(argument => argument.Metadata.Count > 0))
        {
            builder.AppendLine($"{indent}- Argument `{argument.Name}`");
            AppendMetadataList(argument.Metadata, builder, $"{indent}  ");
        }
    }

    private static void AppendMetadataList(IEnumerable<OpenCliMetadata> metadata, StringBuilder builder, string indent = "")
    {
        foreach (var item in metadata)
        {
            if (TryFormatInlineMetadataValue(item, out var inlineValue))
            {
                builder.Append(indent);
                builder.Append("- `");
                builder.Append(item.Name);
                builder.Append("`: ");
                builder.AppendLine(inlineValue);
                continue;
            }

            builder.Append(indent);
            builder.Append("- `");
            builder.Append(item.Name);
            builder.AppendLine("`:");
            AppendMetadataBlockValue(item, builder, $"{indent}  ");
        }

        if (string.IsNullOrEmpty(indent))
        {
            builder.AppendLine();
        }
    }

    private static bool TryFormatInlineMetadataValue(OpenCliMetadata metadata, out string value)
    {
        if (metadata.Value is null)
        {
            value = "null";
            return true;
        }

        switch (metadata.Value.GetValueKind())
        {
            case JsonValueKind.String:
                value = $"`{metadata.Value.GetValue<string>()}`";
                return true;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                value = $"`{metadata.Value}`";
                return true;
            default:
                value = string.Empty;
                return false;
        }
    }

    private static void AppendMetadataBlockValue(OpenCliMetadata metadata, StringBuilder builder, string indent)
    {
        builder.Append(indent);
        builder.AppendLine("```json");
        foreach (var line in metadata.Value!.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
                     .Replace("\r\n", "\n", StringComparison.Ordinal)
                     .Split('\n'))
        {
            builder.Append(indent);
            builder.AppendLine(line);
        }

        builder.Append(indent);
        builder.AppendLine("```");
    }
}
