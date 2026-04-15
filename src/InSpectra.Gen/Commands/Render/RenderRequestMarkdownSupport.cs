using InSpectra.Gen.Cli;
using InSpectra.Lib;
using InSpectra.Lib.Rendering.Contracts;
using InSpectra.Gen.Commands.Common;

namespace InSpectra.Gen.Commands.Render;

internal static class RenderRequestMarkdownSupport
{
    public static RenderExecutionOptions CreateOptions(
        CommonCommandSettings settings,
        ResolvedOutputMode outputMode,
        string? layoutValue,
        string? outputFile,
        string? outputDirectory,
        int? timeoutSeconds,
        bool hasTimeoutSupport,
        int? splitDepth)
    {
        var layout = ResolveLayout(layoutValue);

        ValidateLayoutOutputCombination(layout, outputMode, outputFile, outputDirectory, splitDepth);

        if (hasTimeoutSupport && timeoutSeconds is <= 0)
        {
            throw new CliUsageException("`--timeout` must be greater than zero.");
        }

        return new RenderExecutionOptions(
            layout,
            settings.DryRun,
            settings.IncludeHidden,
            settings.IncludeMetadata,
            settings.Overwrite,
            SingleFile: false,
            CompressLevel: 0,
            CommandValueResolver.NormalizePath(outputFile),
            CommandValueResolver.NormalizePath(outputDirectory));
    }

    public static MarkdownRenderOptions? CreateRenderOptions(
        MarkdownCommandSettingsBase settings,
        RenderLayout layout,
        int? splitDepth)
    {
        var title = CommandValueResolver.NormalizeText(settings.Title);
        var commandPrefix = CommandValueResolver.NormalizeText(settings.CommandPrefix);
        if (layout != RenderLayout.Hybrid && title is null && commandPrefix is null)
        {
            return null;
        }

        return new MarkdownRenderOptions(splitDepth ?? 1, title, commandPrefix);
    }

    private static RenderLayout ResolveLayout(string? layoutValue)
    {
        var normalized = layoutValue?.Trim().ToLowerInvariant();
        return normalized switch
        {
            null or "" or "single" => RenderLayout.Single,
            "tree" => RenderLayout.Tree,
            "hybrid" => RenderLayout.Hybrid,
            _ => throw new CliUsageException("`--layout` must be `single`, `tree`, or `hybrid`."),
        };
    }

    private static void ValidateLayoutOutputCombination(
        RenderLayout layout,
        ResolvedOutputMode outputMode,
        string? outputFile,
        string? outputDirectory,
        int? splitDepth)
    {
        if (!string.IsNullOrWhiteSpace(outputFile) && !string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new CliUsageException("Use either `--out` or `--out-dir`, not both.");
        }

        if (layout == RenderLayout.Single && !string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new CliUsageException("`--out-dir` is only valid with `--layout tree` or `--layout hybrid`.");
        }

        if ((layout == RenderLayout.Tree || layout == RenderLayout.Hybrid)
            && !string.IsNullOrWhiteSpace(outputFile))
        {
            throw new CliUsageException("`--out` is only valid with `--layout single`.");
        }

        if ((layout == RenderLayout.Tree || layout == RenderLayout.Hybrid)
            && string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new CliUsageException($"`--layout {layout.ToString().ToLowerInvariant()}` requires `--out-dir`.");
        }

        if (splitDepth is not null && layout != RenderLayout.Hybrid)
        {
            throw new CliUsageException("`--split-depth` is only valid with `--layout hybrid`.");
        }

        if (splitDepth is <= 0)
        {
            throw new CliUsageException("`--split-depth` must be at least 1.");
        }

        if (outputMode == ResolvedOutputMode.Json
            && layout == RenderLayout.Single
            && string.IsNullOrWhiteSpace(outputFile))
        {
            throw new CliUsageException("Machine output requires `--out` for single-file rendering.");
        }
    }
}
