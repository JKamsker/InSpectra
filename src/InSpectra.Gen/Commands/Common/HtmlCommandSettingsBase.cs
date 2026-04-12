using System.ComponentModel;

namespace InSpectra.Gen.Commands.Common;

/// <summary>
/// Shared HTML-output settings for file render commands.
/// </summary>
public abstract class HtmlCommandSettingsBase : CommonCommandSettings
{
    [Description("Directory where the HTML app bundle should be written.")]
    [Spectre.Console.Cli.CommandOption("--out-dir <DIR>")]
    public string? OutputDirectory { get; init; }

    [Description("Show the Home button in the generated static HTML toolbar.")]
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

    [Description("Allow ?opencli= or ?dir= to load alternate inputs in generated static HTML, with optional ?xmldoc= enrichment. When enabled, query parameters override the embedded input.")]
    [Spectre.Console.Cli.CommandOption("--enable-url")]
    public bool EnableUrl { get; init; }

    [Description("Enable the #/browse package browser route, package deep links such as #/pkg/<id>, and the Browse toolbar button in generated static HTML. Requires --show-home.")]
    [Spectre.Console.Cli.CommandOption("--enable-nuget-browser")]
    public bool EnableNugetBrowser { get; init; }

    [Description("Enable the #/import route and import controls in generated static HTML. Requires --show-home.")]
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

    [Description("Compression level: 0 = none, 1 = compress embedded JSON in multi-file bundle mode, 2 = self-extracting single-file bundle (default).")]
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
