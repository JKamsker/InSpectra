using InSpectra.Gen.Acquisition.Contracts.Exceptions;
using InSpectra.Gen.Rendering.Contracts;
using InSpectra.Gen.Commands.Common;

namespace InSpectra.Gen.UseCases.Render;

internal static class RenderRequestMarkdownSupport
{
    public static RenderExecutionOptions CreateOptions(
        CommonCommandSettings settings,
        string? layoutValue,
        string? outputFile,
        string? outputDirectory,
        int? timeoutSeconds,
        bool hasTimeoutSupport,
        int? splitDepth)
    {
        var outputMode = RenderRequestValueResolver.ResolveOutputMode(settings.Json, settings.Output);
        var layout = ResolveLayout(layoutValue);

        ValidateLayoutOutputCombination(layout, outputMode, outputFile, outputDirectory, splitDepth);

        if (hasTimeoutSupport && timeoutSeconds is <= 0)
        {
            throw new CliUsageException("`--timeout` must be greater than zero.");
        }

        return new RenderExecutionOptions(
            layout,
            outputMode,
            settings.DryRun,
            RenderRequestValueResolver.ResolveFlag(settings.Quiet, "INSPECTRA_GEN_QUIET"),
            RenderRequestValueResolver.ResolveFlag(settings.Verbose, "INSPECTRA_GEN_VERBOSE"),
            RenderRequestValueResolver.ResolveNoColor(settings.NoColor),
            settings.IncludeHidden,
            settings.IncludeMetadata,
            settings.Overwrite,
            SingleFile: false,
            CompressLevel: 0,
            RenderRequestValueResolver.NormalizePath(outputFile),
            RenderRequestValueResolver.NormalizePath(outputDirectory));
    }

    public static MarkdownRenderOptions? CreateRenderOptions(
        MarkdownCommandSettingsBase settings,
        RenderLayout layout,
        int? splitDepth)
    {
        var title = RenderRequestValueResolver.NormalizeText(settings.Title);
        var commandPrefix = RenderRequestValueResolver.NormalizeText(settings.CommandPrefix);
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

