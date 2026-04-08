using InSpectra.Gen.Models;

namespace InSpectra.Gen.Services;

/// <summary>
/// Carries per-page context for the hybrid Markdown layout so link targets can be resolved
/// as either anchor references (inlined commands) or relative file paths (emitted group files).
/// </summary>
public sealed class HybridLinkContext
{
    public HybridLinkContext(int splitDepth, string currentPagePath, CommandPathResolver resolver)
    {
        SplitDepth = splitDepth;
        CurrentPagePath = currentPagePath;
        Resolver = resolver;
    }

    public int SplitDepth { get; }

    public string CurrentPagePath { get; }

    public CommandPathResolver Resolver { get; }

    /// <summary>
    /// A command is emitted to its own file when it is a group (has subcommands) and its
    /// depth does not exceed the configured split depth.
    /// </summary>
    public bool HasOwnFile(NormalizedCommand command)
    {
        if (command.Commands.Count == 0)
        {
            return false;
        }

        return DepthOf(command) <= SplitDepth;
    }

    /// <summary>
    /// Returns the href for linking to <paramref name="command"/> from the current page:
    /// either a relative file path (for split groups) or an in-page anchor (for inlined commands).
    /// </summary>
    public string ResolveTarget(NormalizedCommand command)
    {
        if (HasOwnFile(command))
        {
            var target = Resolver.GetCommandRelativePath(command, "md");
            return Resolver.CreateRelativeLink(CurrentPagePath, target);
        }

        return "#command-" + Resolver.CreateAnchorId(command.Path);
    }

    /// <summary>
    /// Returns a new context rebased on the given page path. Used when descending into a
    /// group file so descendants' link targets are computed relative to that file.
    /// </summary>
    public HybridLinkContext ForPage(string newPagePath)
    {
        return new HybridLinkContext(SplitDepth, newPagePath, Resolver);
    }

    public static int DepthOf(NormalizedCommand command)
    {
        return command.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
