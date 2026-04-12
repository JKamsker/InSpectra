using System.Text;
using InSpectra.Lib.OpenCli.Model;
using InSpectra.Lib.Rendering.Pipeline;
using InSpectra.Lib.Rendering.Pipeline.Model;

namespace InSpectra.Lib.Rendering.Markdown;

internal sealed class MarkdownTableRenderer(RenderModelFormatter formatter)
{
    public void AppendArgumentTable(IEnumerable<OpenCliArgument> arguments, StringBuilder builder)
    {
        builder.AppendLine("| Name | Required | Arity | Accepted Values | Group | Description |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var argument in arguments)
        {
            builder.Append("| ");
            builder.Append(EscapeCell(argument.Hidden ? $"{argument.Name} (hidden)" : argument.Name));
            builder.Append(" | ");
            builder.Append(argument.Required ? "Yes" : "No");
            builder.Append(" | ");
            builder.Append(EscapeCell(formatter.FormatArity(argument)));
            builder.Append(" | ");
            builder.Append(EscapeCell(argument.AcceptedValues.Count == 0 ? "—" : string.Join(", ", argument.AcceptedValues)));
            builder.Append(" | ");
            builder.Append(EscapeCell(argument.Group ?? "—"));
            builder.Append(" | ");
            builder.Append(EscapeCell(argument.Description ?? "—"));
            builder.AppendLine(" |");
        }

        builder.AppendLine();
    }

    public void AppendOptionTable(IEnumerable<ResolvedOption> options, StringBuilder builder)
    {
        builder.AppendLine("| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var resolved in options)
        {
            var option = resolved.Option;
            builder.Append("| ");
            builder.Append(EscapeCell(option.Hidden ? $"{option.Name} (hidden)" : option.Name));
            builder.Append(" | ");
            builder.Append(EscapeCell(option.Aliases.Count == 0 ? "—" : string.Join(", ", option.Aliases)));
            builder.Append(" | ");
            builder.Append(EscapeCell(formatter.FormatOptionValue(option)));
            builder.Append(" | ");
            builder.Append(option.Required ? "Yes" : "No");
            builder.Append(" | ");
            builder.Append(option.Recursive ? "Yes" : "No");
            builder.Append(" | ");
            builder.Append(EscapeCell(resolved.IsInherited ? $"Inherited from {resolved.InheritedFromPath}" : "Declared"));
            builder.Append(" | ");
            builder.Append(EscapeCell(option.Group ?? "—"));
            builder.Append(" | ");
            builder.Append(EscapeCell(option.Description ?? "—"));
            builder.Append(" | ");
            builder.Append(EscapeCell(FormatOptionArguments(option.Arguments)));
            builder.AppendLine(" |");
        }

        builder.AppendLine();
    }

    public void AppendExitCodeTable(IEnumerable<OpenCliExitCode> exitCodes, StringBuilder builder)
    {
        builder.AppendLine("| Code | Description |");
        builder.AppendLine("| --- | --- |");
        foreach (var exitCode in exitCodes)
        {
            builder.AppendLine($"| `{exitCode.Code}` | {EscapeCell(exitCode.Description ?? "—")} |");
        }

        builder.AppendLine();
    }

    private static string EscapeCell(string value)
    {
        return value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace("|", "\\|", StringComparison.Ordinal);
    }

    private string FormatOptionArguments(IReadOnlyList<OpenCliArgument> arguments)
    {
        if (arguments.Count == 0)
        {
            return "—";
        }

        return string.Join("<br/>", arguments.Select(FormatOptionArgument));
    }

    private string FormatOptionArgument(OpenCliArgument argument)
    {
        var details = new List<string>
        {
            argument.Hidden ? $"{argument.Name} (hidden)" : argument.Name,
            argument.Required ? "required" : "optional",
            $"arity {formatter.FormatArity(argument)}",
        };

        if (argument.AcceptedValues.Count > 0)
        {
            details.Add($"accepted {string.Join(", ", argument.AcceptedValues)}");
        }

        if (!string.IsNullOrWhiteSpace(argument.Group))
        {
            details.Add($"group {argument.Group}");
        }

        if (!string.IsNullOrWhiteSpace(argument.Description))
        {
            details.Add(argument.Description);
        }

        return string.Join(" · ", details);
    }
}
