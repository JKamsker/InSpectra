namespace InSpectra.Lib.Rendering.Contracts;

public sealed record HtmlFeatureFlags(
    bool ShowHome,
    bool Composer,
    bool DarkTheme,
    bool LightTheme,
    bool UrlLoading,
    bool NugetBrowser,
    bool PackageUpload,
    bool ColorThemePicker);
