using InSpectra.Gen.Cli;
using InSpectra.Lib;
using InSpectra.Lib.Rendering.Contracts;
using InSpectra.Gen.Commands.Common;

namespace InSpectra.Gen.Commands.Render;

public static class RenderRequestFactory
{
    public static RenderExecutionOptions CreateMarkdownOptions(
        CommonCommandSettings settings,
        ResolvedOutputMode outputMode,
        string? layoutValue,
        string? outputFile,
        string? outputDirectory,
        int? timeoutSeconds,
        bool hasTimeoutSupport,
        int? splitDepth = null)
        => RenderRequestMarkdownSupport.CreateOptions(
            settings,
            outputMode,
            layoutValue,
            outputFile,
            outputDirectory,
            timeoutSeconds,
            hasTimeoutSupport,
            splitDepth);

    public static MarkdownRenderOptions? CreateMarkdownRenderOptions(
        MarkdownCommandSettingsBase settings,
        RenderLayout layout,
        int? splitDepth)
        => RenderRequestMarkdownSupport.CreateRenderOptions(settings, layout, splitDepth);

    public static RenderExecutionOptions CreateHtmlOptions(
        HtmlCommandSettingsBase settings,
        string? layoutValue,
        string? outputFile,
        string? outputDirectory,
        int? timeoutSeconds,
        bool hasTimeoutSupport)
        => RenderRequestHtmlSupport.CreateOptions(
            settings,
            layoutValue,
            outputFile,
            outputDirectory,
            timeoutSeconds,
            hasTimeoutSupport);

    public static HtmlFeatureFlags CreateHtmlFeatureFlags(HtmlCommandSettingsBase settings)
        => RenderRequestHtmlSupport.CreateFeatureFlags(settings);

    public static HtmlThemeOptions CreateHtmlThemeOptions(HtmlCommandSettingsBase settings)
        => RenderRequestHtmlSupport.CreateThemeOptions(settings);
}
