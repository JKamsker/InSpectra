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
    {
        var outputMode = ResolveOutputMode(settings);
        var layout = ResolveMarkdownLayout(layoutValue);

        ValidateMarkdownLayoutOutputCombination(layout, outputMode, outputFile, outputDirectory, splitDepth);

        if (hasTimeoutSupport && timeoutSeconds is <= 0)
        {
            throw new CliUsageException("`--timeout` must be greater than zero.");
        }

        return new RenderExecutionOptions(
            layout,
            outputMode,
            settings.DryRun,
            ResolveFlag(settings.Quiet, "INSPECTRA_GEN_QUIET"),
            ResolveFlag(settings.Verbose, "INSPECTRA_GEN_VERBOSE"),
            settings.NoColor || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NO_COLOR")),
            settings.IncludeHidden,
            settings.IncludeMetadata,
            settings.Overwrite,
            SingleFile: false,
            CompressLevel: 0,
            NormalizePath(outputFile),
            NormalizePath(outputDirectory));
    }

    public static MarkdownRenderOptions? CreateMarkdownRenderOptions(RenderLayout layout, int? splitDepth)
    {
        if (layout != RenderLayout.Hybrid)
        {
            return null;
        }

        return new MarkdownRenderOptions(splitDepth ?? 1);
    }

    public static RenderExecutionOptions CreateHtmlOptions(
        HtmlCommandSettingsBase settings,
        string? layoutValue,
        string? outputFile,
        string? outputDirectory,
        int? timeoutSeconds,
        bool hasTimeoutSupport)
    {
        var outputMode = ResolveOutputMode(settings);

        ValidateHtmlOutputCombination(layoutValue, outputFile, outputDirectory);

        if (hasTimeoutSupport && timeoutSeconds is <= 0)
        {
            throw new CliUsageException("`--timeout` must be greater than zero.");
        }

        var compressLevel = ResolveCompressLevel(settings.CompressionLevel, settings.SingleFile);

        return new RenderExecutionOptions(
            RenderLayout.App,
            outputMode,
            settings.DryRun,
            ResolveFlag(settings.Quiet, "INSPECTRA_GEN_QUIET"),
            ResolveFlag(settings.Verbose, "INSPECTRA_GEN_VERBOSE"),
            settings.NoColor || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NO_COLOR")),
            settings.IncludeHidden,
            settings.IncludeMetadata,
            settings.Overwrite,
            settings.SingleFile || compressLevel >= 2,
            compressLevel,
            OutputFile: null,
            NormalizePath(outputDirectory));
    }

    public static HtmlFeatureFlags CreateHtmlFeatureFlags(HtmlCommandSettingsBase settings)
    {
        return CreateHtmlFeatureFlags(
            settings.ShowHome,
            settings.NoComposer,
            settings.NoDark,
            settings.NoLight,
            settings.EnableUrl,
            settings.EnableNugetBrowser,
            settings.EnablePackageUpload,
            settings.NoThemePicker);
    }

    public static HtmlFeatureFlags CreateHtmlFeatureFlags(SelfDocHtmlCommandSettingsBase settings)
    {
        return CreateHtmlFeatureFlags(
            settings.ShowHome,
            settings.NoComposer,
            settings.NoDark,
            settings.NoLight,
            settings.EnableUrl,
            settings.EnableNugetBrowser,
            settings.EnablePackageUpload,
            settings.NoThemePicker);
    }

    public static HtmlThemeOptions CreateHtmlThemeOptions(HtmlCommandSettingsBase settings)
    {
        return CreateHtmlThemeOptions(settings.Theme, settings.ColorTheme, settings.Accent, settings.AccentDark);
    }

    public static HtmlThemeOptions CreateHtmlThemeOptions(SelfDocHtmlCommandSettingsBase settings)
    {
        return CreateHtmlThemeOptions(settings.Theme, settings.ColorTheme, settings.Accent, settings.AccentDark);
    }

    private static HtmlThemeOptions CreateHtmlThemeOptions(string? theme, string? colorTheme, string? accent, string? accentDark)
    {
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

    private static HtmlFeatureFlags CreateHtmlFeatureFlags(
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

    public static string ResolveWorkingDirectory(string? workingDirectory)
    {
        var resolved = string.IsNullOrWhiteSpace(workingDirectory)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(workingDirectory);

        if (!Directory.Exists(resolved))
        {
            throw new CliUsageException($"Working directory `{resolved}` does not exist.");
        }

        return resolved;
    }

    public static int ResolveTimeoutSeconds(int? timeoutSeconds, int defaultSeconds = 30)
    {
        if (timeoutSeconds is > 0)
        {
            return timeoutSeconds.Value;
        }

        var envValue = Environment.GetEnvironmentVariable("INSPECTRA_GEN_TIMEOUT");
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            if (!int.TryParse(envValue, out var parsed) || parsed <= 0)
            {
                throw new CliUsageException("`INSPECTRA_GEN_TIMEOUT` must be a positive integer.");
            }

            return parsed;
        }

        return defaultSeconds;
    }

    private static ResolvedOutputMode ResolveOutputMode(CommonCommandSettings settings)
    {
        var explicitOutput = settings.Output?.Trim().ToLowerInvariant();
        var envOutput = Environment.GetEnvironmentVariable("INSPECTRA_GEN_OUTPUT")?.Trim().ToLowerInvariant();

        if (settings.Json && explicitOutput is "human")
        {
            throw new CliUsageException("`--json` cannot be combined with `--output human`.");
        }

        if (explicitOutput is not null and not ("human" or "json"))
        {
            throw new CliUsageException("`--output` must be `human` or `json`.");
        }

        if (envOutput is not null and not ("human" or "json"))
        {
            throw new CliUsageException("`INSPECTRA_GEN_OUTPUT` must be `human` or `json`.");
        }

        if (explicitOutput is "json" || settings.Json)
        {
            return ResolvedOutputMode.Json;
        }

        if (explicitOutput is "human")
        {
            return ResolvedOutputMode.Human;
        }

        return envOutput == "json"
            ? ResolvedOutputMode.Json
            : ResolvedOutputMode.Human;
    }

    private static RenderLayout ResolveMarkdownLayout(string? layoutValue)
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

    private static void ValidateMarkdownLayoutOutputCombination(
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

        if (layout == RenderLayout.Tree && !string.IsNullOrWhiteSpace(outputFile))
        {
            throw new CliUsageException("`--out` is only valid with `--layout single`.");
        }

        if (layout == RenderLayout.Hybrid && !string.IsNullOrWhiteSpace(outputFile))
        {
            throw new CliUsageException("`--out` is only valid with `--layout single`.");
        }

        if (layout == RenderLayout.Tree && string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new CliUsageException("`--layout tree` requires `--out-dir`.");
        }

        if (layout == RenderLayout.Hybrid && string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new CliUsageException("`--layout hybrid` requires `--out-dir`.");
        }

        if (splitDepth is not null && layout != RenderLayout.Hybrid)
        {
            throw new CliUsageException("`--split-depth` is only valid with `--layout hybrid`.");
        }

        if (splitDepth is <= 0)
        {
            throw new CliUsageException("`--split-depth` must be at least 1.");
        }

        if (outputMode == ResolvedOutputMode.Json && layout == RenderLayout.Single && string.IsNullOrWhiteSpace(outputFile))
        {
            throw new CliUsageException("Machine output requires `--out` for single-file rendering.");
        }
    }

    private static void ValidateHtmlOutputCombination(
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

    private static bool ResolveFlag(bool flag, string environmentVariable)
    {
        if (flag)
        {
            return true;
        }

        var value = Environment.GetEnvironmentVariable(environmentVariable);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" => true,
            "0" or "false" or "no" => false,
            _ => throw new CliUsageException($"`{environmentVariable}` must be a boolean value."),
        };
    }

    private static string? NormalizePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? null
            : Path.GetFullPath(path);
    }
}
