using System.ComponentModel;

namespace InSpectra.Gen.Runtime.Settings;

/// <summary>
/// Shared HTML-output settings for file render commands.
/// </summary>
public abstract class HtmlCommandSettingsBase : CommonCommandSettings
{
    [Description("Directory where the HTML app bundle should be written.")]
    [Spectre.Console.Cli.CommandOption("--out-dir <DIR>")]
    public string? OutputDirectory { get; init; }

    [Description("Show the viewer home screen button in the generated HTML app.")]
    [Spectre.Console.Cli.CommandOption("--show-home")]
    public bool ShowHome { get; init; }

    [Description("Hide the interactive command composer from the generated HTML app.")]
    [Spectre.Console.Cli.CommandOption("--no-composer")]
    public bool NoComposer { get; init; }

    [Description("Disable dark mode in the generated HTML app.")]
    [Spectre.Console.Cli.CommandOption("--no-dark")]
    public bool NoDark { get; init; }

    [Description("Disable light mode in the generated HTML app.")]
    [Spectre.Console.Cli.CommandOption("--no-light")]
    public bool NoLight { get; init; }

    [Description("Allow the viewer to load OpenCLI inputs from URL query parameters.")]
    [Spectre.Console.Cli.CommandOption("--enable-url")]
    public bool EnableUrl { get; init; }

    [Description("Enable the NuGet package browser on the viewer home screen.")]
    [Spectre.Console.Cli.CommandOption("--enable-nuget-browser")]
    public bool EnableNugetBrowser { get; init; }

    [Description("Enable local package upload on the viewer home screen.")]
    [Spectre.Console.Cli.CommandOption("--enable-package-upload")]
    public bool EnablePackageUpload { get; init; }

    [Description("Custom label shown in the viewer header (e.g. a version string).")]
    [Spectre.Console.Cli.CommandOption("--label <TEXT>")]
    public string? Label { get; init; }

    [Description("Override the CLI title shown in the viewer header and overview.")]
    [Spectre.Console.Cli.CommandOption("--title <TEXT>")]
    public string? Title { get; init; }

    [Description("Override the CLI command prefix used in generated examples and the composer.")]
    [Spectre.Console.Cli.CommandOption("--command-prefix <TEXT>")]
    public string? CommandPrefix { get; init; }

    [Description("Emit a single self-contained HTML file with all assets inlined. Works from file:// without a web server.")]
    [Spectre.Console.Cli.CommandOption("--single-file")]
    public bool SingleFile { get; init; }

    [Description("Compression level: 0 = none, 1 = compress embedded JSON (default bundle mode), 2 = self-extracting single-file bundle.")]
    [Spectre.Console.Cli.CommandOption("--compression-level <LEVEL>")]
    public int? CompressionLevel { get; init; }

    [Description("Set the initial theme mode (light or dark).")]
    [Spectre.Console.Cli.CommandOption("--theme <MODE>")]
    public string? Theme { get; init; }

    [Description("Set the color theme (cyan, indigo, emerald, amber, rose, blue).")]
    [Spectre.Console.Cli.CommandOption("--color-theme <NAME>")]
    public string? ColorTheme { get; init; }

    [Description("Custom accent color for light mode (hex, e.g. \"#7c3aed\").")]
    [Spectre.Console.Cli.CommandOption("--accent <COLOR>")]
    public string? Accent { get; init; }

    [Description("Custom accent color for dark mode (hex). Falls back to --accent if omitted.")]
    [Spectre.Console.Cli.CommandOption("--accent-dark <COLOR>")]
    public string? AccentDark { get; init; }

    [Description("Hide the color theme picker from the viewer toolbar.")]
    [Spectre.Console.Cli.CommandOption("--no-theme-picker")]
    public bool NoThemePicker { get; init; }
}
