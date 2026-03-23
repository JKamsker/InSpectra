using OpenCli.Renderer.Models;

namespace OpenCli.Renderer.Services;

public sealed class OpenCliNormalizer
{
    public NormalizedCliDocument Normalize(OpenCliDocument document, bool includeHidden)
    {
        var visibleRootArguments = document.Arguments.Where(argument => includeHidden || !argument.Hidden).ToList();
        var visibleRootOptions = document.Options.Where(option => includeHidden || !option.Hidden).ToList();
        var inherited = visibleRootOptions
            .Where(option => option.Recursive)
            .Select(option => new InheritedOption(option, "<root>"))
            .ToList();

        var commands = document.Commands
            .Where(command => includeHidden || !command.Hidden)
            .Select(command => NormalizeCommand(command, null, inherited, includeHidden))
            .ToList();

        return new NormalizedCliDocument
        {
            Source = document,
            RootArguments = visibleRootArguments,
            RootOptions = visibleRootOptions,
            Commands = commands,
        };
    }

    private NormalizedCommand NormalizeCommand(
        OpenCliCommand command,
        string? parentPath,
        IReadOnlyList<InheritedOption> inheritedOptions,
        bool includeHidden)
    {
        var path = string.IsNullOrWhiteSpace(parentPath)
            ? command.Name
            : $"{parentPath} {command.Name}";
        var arguments = command.Arguments.Where(argument => includeHidden || !argument.Hidden).ToList();
        var declaredOptions = command.Options.Where(option => includeHidden || !option.Hidden).ToList();
        var resolvedInheritedOptions = ResolveInheritedOptions(inheritedOptions, declaredOptions);
        var nextInherited = inheritedOptions
            .Concat(declaredOptions.Where(option => option.Recursive).Select(option => new InheritedOption(option, path)))
            .ToList();
        var childCommands = command.Commands
            .Where(child => includeHidden || !child.Hidden)
            .Select(child => NormalizeCommand(child, path, nextInherited, includeHidden))
            .ToList();

        return new NormalizedCommand
        {
            Path = path,
            Command = command,
            Arguments = arguments,
            DeclaredOptions = declaredOptions,
            InheritedOptions = resolvedInheritedOptions,
            Commands = childCommands,
        };
    }

    private static IReadOnlyList<ResolvedOption> ResolveInheritedOptions(
        IReadOnlyList<InheritedOption> inheritedOptions,
        IReadOnlyList<OpenCliOption> declaredOptions)
    {
        var seen = new HashSet<string>(declaredOptions.Select(CreateOptionKey), StringComparer.OrdinalIgnoreCase);
        var buffer = new List<ResolvedOption>();

        for (var index = inheritedOptions.Count - 1; index >= 0; index--)
        {
            var inherited = inheritedOptions[index];
            var key = CreateOptionKey(inherited.Option);
            if (!seen.Add(key))
            {
                continue;
            }

            buffer.Add(new ResolvedOption
            {
                Option = inherited.Option,
                IsInherited = true,
                InheritedFromPath = inherited.SourcePath,
            });
        }

        buffer.Reverse();
        return buffer;
    }

    private static string CreateOptionKey(OpenCliOption option)
    {
        return option.Name;
    }

    private sealed record InheritedOption(OpenCliOption Option, string SourcePath);
}
