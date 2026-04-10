using System.Text.Json;
using InSpectra.Gen.Models;
using InSpectra.Gen.Runtime.Json;
using InSpectra.Gen.Runtime.Rendering;

namespace InSpectra.Gen.Rendering.Html;

internal static class HtmlBundleBootstrapSupport
{
    public static string BuildRawBootstrapJson(
        AcquiredRenderDocument prepared,
        bool includeHidden,
        bool includeMetadata,
        HtmlFeatureFlags features,
        string? label,
        string? title,
        string? commandPrefix,
        HtmlThemeOptions? themeOptions)
    {
        var payload = new InlineBootstrap
        {
            Mode = "inline",
            OpenCli = prepared.RawDocument,
            XmlDoc = prepared.XmlDocument,
            Options = new ViewerOptionsPayload
            {
                IncludeHidden = includeHidden,
                IncludeMetadata = includeMetadata,
                Label = label,
                Title = title,
                CommandPrefix = commandPrefix,
                Theme = themeOptions?.Theme,
                ColorTheme = themeOptions?.ColorTheme,
                CustomAccent = themeOptions?.CustomAccent,
                CustomAccentDark = themeOptions?.CustomAccentDark,
            },
            Features = new FeatureFlagsPayload
            {
                ShowHome = features.ShowHome,
                Composer = features.Composer,
                DarkTheme = features.DarkTheme,
                LightTheme = features.LightTheme,
                UrlLoading = features.UrlLoading,
                NugetBrowser = features.NugetBrowser,
                PackageUpload = features.PackageUpload,
                ColorThemePicker = features.ColorThemePicker,
            },
        };

        return JsonSerializer.Serialize(payload, JsonOutput.CompactSerializerOptions);
    }

    public static string EscapeForJsStringLiteral(string value)
        => value
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("</script", "<\\/script", StringComparison.OrdinalIgnoreCase);

    private sealed class InlineBootstrap
    {
        public required string Mode { get; init; }

        public required OpenCliDocument OpenCli { get; init; }

        public string? XmlDoc { get; init; }

        public required ViewerOptionsPayload Options { get; init; }

        public required FeatureFlagsPayload Features { get; init; }
    }

    private sealed class ViewerOptionsPayload
    {
        public required bool IncludeHidden { get; init; }

        public required bool IncludeMetadata { get; init; }

        public string? Label { get; init; }

        public string? Title { get; init; }

        public string? CommandPrefix { get; init; }

        public string? Theme { get; init; }

        public string? ColorTheme { get; init; }

        public string? CustomAccent { get; init; }

        public string? CustomAccentDark { get; init; }
    }

    private sealed class FeatureFlagsPayload
    {
        public required bool ShowHome { get; init; }

        public required bool Composer { get; init; }

        public required bool DarkTheme { get; init; }

        public required bool LightTheme { get; init; }

        public required bool UrlLoading { get; init; }

        public required bool NugetBrowser { get; init; }

        public required bool PackageUpload { get; init; }

        public required bool ColorThemePicker { get; init; }
    }
}

