using InSpectra.Gen.Acquisition.Contracts;

using InSpectra.Gen.Acquisition.Contracts.Exceptions;
using InSpectra.Gen.Runtime.Acquisition;
using InSpectra.Gen.Runtime.Rendering;
using InSpectra.Gen.Runtime.Settings;

namespace InSpectra.Gen.Runtime;

public static class RenderRequestFactory
{
    public static RenderExecutionOptions CreateMarkdownOptions(
        CommonCommandSettings settings,
        string? layoutValue,
        string? outputFile,
        string? outputDirectory,
        int? timeoutSeconds,
        bool hasTimeoutSupport,
        int? splitDepth = null)
        => RenderRequestMarkdownSupport.CreateOptions(
            settings,
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

    public static string ResolveWorkingDirectory(string? workingDirectory)
        => RenderRequestValueResolver.ResolveWorkingDirectory(workingDirectory);

    public static int ResolveTimeoutSeconds(int? timeoutSeconds, int defaultSeconds = 30)
        => RenderRequestValueResolver.ResolveTimeoutSeconds(timeoutSeconds, defaultSeconds);

    public static ResolvedOutputMode ResolveOutputMode(GenerateCommandSettingsBase settings)
        => RenderRequestValueResolver.ResolveOutputMode(settings.Json, settings.Output);

    public static OpenCliMode ResolveOpenCliMode(string? value, OpenCliMode defaultMode)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return defaultMode;
        }

        return normalized switch
        {
            AnalysisMode.Native => OpenCliMode.Native,
            AnalysisMode.Auto => OpenCliMode.Auto,
            AnalysisMode.Help => OpenCliMode.Help,
            AnalysisMode.CliFx => OpenCliMode.CliFx,
            AnalysisMode.Static => OpenCliMode.Static,
            AnalysisMode.Hook => OpenCliMode.Hook,
            _ => throw new CliUsageException("`--opencli-mode` must be `native`, `auto`, `help`, `clifx`, `static`, or `hook`."),
        };
    }

}
