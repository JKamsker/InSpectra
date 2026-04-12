using InSpectra.Lib.OpenCli.Model;
using InSpectra.Lib.Rendering.Pipeline.Model;

namespace InSpectra.Lib.Rendering.Pipeline;

internal sealed class RenderModelFormatter
{
    public string FormatArity(OpenCliArgument argument)
    {
        var minimum = argument.Arity?.Minimum ?? (argument.Required ? 1 : 0);
        var maximum = argument.Arity?.Maximum;
        return maximum switch
        {
            null => $"{minimum}..n",
            _ when minimum == maximum => minimum.ToString(),
            _ => $"{minimum}..{maximum}",
        };
    }

    public string FormatOptionValue(OpenCliOption option)
    {
        if (option.Arguments.Count == 0)
        {
            return "flag";
        }

        return string.Join(' ', option.Arguments.Select(argument =>
        {
            var maximum = argument.Arity?.Maximum;
            return maximum is null or > 1
                ? $"<{argument.Name}...>"
                : $"<{argument.Name}>";
        }));
    }

    public IReadOnlyList<string> BuildCommandAttributes(OpenCliCommand command)
    {
        var attributes = new List<string>();
        if (command.Aliases.Count > 0)
        {
            attributes.Add($"Aliases: {string.Join(", ", command.Aliases)}");
        }

        if (command.Interactive)
        {
            attributes.Add("Interactive command");
        }

        if (command.Hidden)
        {
            attributes.Add("Hidden command");
        }

        return attributes;
    }

    public string FormatDescriptionSuffix(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? string.Empty : $" — {description}";
    }

    public string? TryGetClrType(IEnumerable<OpenCliMetadata> metadata)
    {
        var value = metadata.FirstOrDefault(item => string.Equals(item.Name, "ClrType", StringComparison.OrdinalIgnoreCase))?.Value;
        return value is not null && value.GetValueKind() == System.Text.Json.JsonValueKind.String
            ? value.GetValue<string>()
            : null;
    }
}
