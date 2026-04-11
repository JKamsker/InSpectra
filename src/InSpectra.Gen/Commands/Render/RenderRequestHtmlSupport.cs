using InSpectra.Gen.Core;
using InSpectra.Gen.Rendering.Contracts;
using InSpectra.Gen.Commands.Common;

namespace InSpectra.Gen.Commands.Render;

internal static class RenderRequestHtmlSupport
{
    public static RenderExecutionOptions CreateOptions(
        HtmlCommandSettingsBase settings,
        string? layoutValue,
        string? outputFile,
        string? outputDirectory,
        int? timeoutSeconds,
        bool hasTimeoutSupport)
    {
        var outputMode = RenderRequestValueResolver.ResolveOutputMode(settings.Json, settings.Output);

        ValidateOutputCombination(layoutValue, outputFile, outputDirectory);

        if (hasTimeoutSupport && timeoutSeconds is <= 0)
        {
            throw new CliUsageException("`--timeout` must be greater than zero.");
        }

        var compressLevel = ResolveCompressLevel(settings.CompressionLevel, settings.SingleFile);
        var singleFile = settings.SingleFile || compressLevel >= 2;

        return new RenderExecutionOptions(
            RenderLayout.App,
            outputMode,
            settings.DryRun,
            RenderRequestValueResolver.ResolveFlag(settings.Quiet, "INSPECTRA_GEN_QUIET"),
            RenderRequestValueResolver.ResolveFlag(settings.Verbose, "INSPECTRA_GEN_VERBOSE"),
            RenderRequestValueResolver.ResolveNoColor(settings.NoColor),
            settings.IncludeHidden,
            settings.IncludeMetadata,
            settings.Overwrite,
            singleFile,
            compressLevel,
            OutputFile: null,
            RenderRequestValueResolver.NormalizePath(outputDirectory));
    }

    public static HtmlFeatureFlags CreateFeatureFlags(HtmlCommandSettingsBase settings)
        => CreateFeatureFlags(
            settings.ShowHome,
            settings.NoComposer,
            settings.NoDark,
            settings.NoLight,
            settings.EnableUrl,
            settings.EnableNugetBrowser,
            settings.EnablePackageUpload,
            settings.NoThemePicker);

    public static HtmlThemeOptions CreateThemeOptions(HtmlCommandSettingsBase settings)
        => CreateThemeOptions(settings.Theme, settings.ColorTheme, settings.Accent, settings.AccentDark);

    private static HtmlThemeOptions CreateThemeOptions(
        string? theme,
        string? colorTheme,
        string? accent,
        string? accentDark)
    {
        theme = NormalizeOptionalValue(theme);
        colorTheme = NormalizeOptionalValue(colorTheme);
        accent = NormalizeOptionalValue(accent);
        accentDark = NormalizeOptionalValue(accentDark);

        if (theme is not null and not ("light" or "dark"))
        {
            throw new CliUsageException("`--theme` must be `light` or `dark`.");
        }

        if (accentDark is not null && accent is null)
        {
            throw new CliUsageException("`--accent-dark` requires `--accent`.");
        }

        return new HtmlThemeOptions(
            Theme: theme,
            ColorTheme: colorTheme,
            CustomAccent: accent,
            CustomAccentDark: accentDark);
    }

    private static HtmlFeatureFlags CreateFeatureFlags(
        bool showHome,
        bool noComposer,
        bool noDark,
        bool noLight,
        bool enableUrl,
        bool enableNugetBrowser,
        bool enablePackageUpload,
        bool noThemePicker)
    {
        if (noDark && noLight)
        {
            throw new CliUsageException("`--no-dark` and `--no-light` cannot both be set.");
        }

        if (enableNugetBrowser && !showHome)
        {
            throw new CliUsageException("`--enable-nuget-browser` requires `--show-home`.");
        }

        if (enablePackageUpload && !showHome)
        {
            throw new CliUsageException("`--enable-package-upload` requires `--show-home`.");
        }

        return new HtmlFeatureFlags(
            ShowHome: showHome,
            Composer: !noComposer,
            DarkTheme: !noDark,
            LightTheme: !noLight,
            UrlLoading: enableUrl,
            NugetBrowser: enableNugetBrowser,
            PackageUpload: enablePackageUpload,
            ColorThemePicker: !noThemePicker);
    }

    private static int ResolveCompressLevel(int? explicitLevel, bool singleFile)
    {
        if (explicitLevel is not null)
        {
            if (explicitLevel is < 0 or > 2)
            {
                throw new CliUsageException("`--compression-level` must be 0, 1, or 2.");
            }

            return explicitLevel.Value;
        }

        return 2;
    }

    private static void ValidateOutputCombination(
        string? layoutValue,
        string? outputFile,
        string? outputDirectory)
    {
        if (!string.IsNullOrWhiteSpace(layoutValue))
        {
            throw new CliUsageException("`--layout` is not supported for HTML output. HTML always renders as an app bundle.");
        }

        if (!string.IsNullOrWhiteSpace(outputFile))
        {
            throw new CliUsageException("`--out` is not supported for HTML output. Use `--out-dir`.");
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new CliUsageException("HTML output requires `--out-dir`.");
        }
    }

    private static string? NormalizeOptionalValue(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
