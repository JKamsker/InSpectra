using System.ComponentModel;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Runtime;

/// <summary>
/// Shared CLI flags for the file and exec render commands.
/// </summary>
public abstract class CommonCommandSettings : CommandSettings
{
    /// <summary>
    /// Emit the stable machine-readable JSON envelope instead of human output.
    /// </summary>
    [Description("Emit the stable machine-readable JSON envelope instead of human output.")]
    [CommandOption("--json")]
    public bool Json { get; init; }

    /// <summary>
    /// Override the output mode. Supported values are <c>human</c> and <c>json</c>.
    /// </summary>
    [Description("Override the output mode. Supported values are human and json.")]
    [CommandOption("--output <MODE>")]
    public string? Output { get; init; }

    /// <summary>
    /// Suppress non-essential console output.
    /// </summary>
    [Description("Suppress non-essential console output.")]
    [CommandOption("-q|--quiet")]
    public bool Quiet { get; init; }

    /// <summary>
    /// Increase diagnostic detail in the rendered summary output.
    /// </summary>
    [Description("Increase diagnostic detail in the rendered summary output.")]
    [CommandOption("--verbose")]
    public bool Verbose { get; init; }

    /// <summary>
    /// Disable ANSI color sequences in human-readable console output.
    /// </summary>
    [Description("Disable ANSI color sequences in human-readable console output.")]
    [CommandOption("--no-color")]
    public bool NoColor { get; init; }

    /// <summary>
    /// Preview the resolved render plan without writing files.
    /// </summary>
    [Description("Preview the resolved render plan without writing files.")]
    [CommandOption("--dry-run")]
    public bool DryRun { get; init; }

    /// <summary>
    /// Allow existing output files or directories to be replaced.
    /// </summary>
    [Description("Allow existing output files or directories to be replaced.")]
    [CommandOption("--overwrite")]
    public bool Overwrite { get; init; }

    /// <summary>
    /// Include commands and options marked hidden by the source CLI.
    /// </summary>
    [Description("Include commands and options marked hidden by the source CLI.")]
    [CommandOption("--include-hidden")]
    public bool IncludeHidden { get; init; }

    /// <summary>
    /// Include metadata sections in the rendered Markdown or HTML output.
    /// </summary>
    [Description("Include metadata sections in the rendered Markdown or HTML output.")]
    [CommandOption("--include-metadata")]
    public bool IncludeMetadata { get; init; }
}

/// <summary>
/// Shared HTML-output settings for file and exec render commands.
/// </summary>
public abstract class HtmlCommandSettingsBase : CommonCommandSettings
{
    /// <summary>
    /// Directory where the HTML app bundle should be written.
    /// </summary>
    [Description("Directory where the HTML app bundle should be written.")]
    [CommandOption("--out-dir <DIR>")]
    public string? OutputDirectory { get; init; }

    /// <summary>
    /// Show the viewer home screen button in the generated HTML app.
    /// </summary>
    [Description("Show the viewer home screen button in the generated HTML app.")]
    [CommandOption("--show-home")]
    public bool ShowHome { get; init; }

    /// <summary>
    /// Hide the interactive command composer from the generated HTML app.
    /// </summary>
    [Description("Hide the interactive command composer from the generated HTML app.")]
    [CommandOption("--no-composer")]
    public bool NoComposer { get; init; }

    /// <summary>
    /// Disable dark mode in the generated HTML app.
    /// </summary>
    [Description("Disable dark mode in the generated HTML app.")]
    [CommandOption("--no-dark")]
    public bool NoDark { get; init; }

    /// <summary>
    /// Disable light mode in the generated HTML app.
    /// </summary>
    [Description("Disable light mode in the generated HTML app.")]
    [CommandOption("--no-light")]
    public bool NoLight { get; init; }

    /// <summary>
    /// Allow the viewer to load OpenCLI inputs from URL query parameters.
    /// </summary>
    [Description("Allow the viewer to load OpenCLI inputs from URL query parameters.")]
    [CommandOption("--enable-url")]
    public bool EnableUrl { get; init; }

    /// <summary>
    /// Enable the NuGet package browser on the viewer home screen.
    /// </summary>
    [Description("Enable the NuGet package browser on the viewer home screen.")]
    [CommandOption("--enable-nuget-browser")]
    public bool EnableNugetBrowser { get; init; }

    /// <summary>
    /// Enable local package upload on the viewer home screen.
    /// </summary>
    [Description("Enable local package upload on the viewer home screen.")]
    [CommandOption("--enable-package-upload")]
    public bool EnablePackageUpload { get; init; }

    /// <summary>
    /// Custom label shown in the viewer header (e.g. a version string).
    /// </summary>
    [Description("Custom label shown in the viewer header (e.g. a version string).")]
    [CommandOption("--label <TEXT>")]
    public string? Label { get; init; }

    /// <summary>
    /// Override the CLI title shown in the viewer header and overview.
    /// </summary>
    [Description("Override the CLI title shown in the viewer header and overview.")]
    [CommandOption("--title <TEXT>")]
    public string? Title { get; init; }

    /// <summary>
    /// Override the CLI command prefix used in generated examples and the composer.
    /// </summary>
    [Description("Override the CLI command prefix used in generated examples and the composer.")]
    [CommandOption("--command-prefix <TEXT>")]
    public string? CommandPrefix { get; init; }

    /// <summary>
    /// Emit a single self-contained HTML file with all assets inlined. Works from file:// without a web server.
    /// </summary>
    [Description("Emit a single self-contained HTML file with all assets inlined. Works from file:// without a web server.")]
    [CommandOption("--single-file")]
    public bool SingleFile { get; init; }

    /// <summary>
    /// Compression level for the output: 0 = none, 1 = compress embedded JSON, 2 = self-extracting bundle (default 2).
    /// </summary>
    [Description("Compression level: 0 = none, 1 = compress embedded JSON, 2 = self-extracting bundle (default 2).")]
    [CommandOption("--compression-level <LEVEL>")]
    public int? CompressionLevel { get; init; }

    /// <summary>
    /// Set the initial theme mode (light or dark).
    /// </summary>
    [Description("Set the initial theme mode (light or dark).")]
    [CommandOption("--theme <MODE>")]
    public string? Theme { get; init; }

    /// <summary>
    /// Set the color theme (cyan, indigo, emerald, amber, rose, blue).
    /// </summary>
    [Description("Set the color theme (cyan, indigo, emerald, amber, rose, blue).")]
    [CommandOption("--color-theme <NAME>")]
    public string? ColorTheme { get; init; }

    /// <summary>
    /// Custom accent color for light mode (hex, e.g. "#7c3aed").
    /// </summary>
    [Description("Custom accent color for light mode (hex, e.g. \"#7c3aed\").")]
    [CommandOption("--accent <COLOR>")]
    public string? Accent { get; init; }

    /// <summary>
    /// Custom accent color for dark mode (hex). Falls back to --accent if omitted.
    /// </summary>
    [Description("Custom accent color for dark mode (hex). Falls back to --accent if omitted.")]
    [CommandOption("--accent-dark <COLOR>")]
    public string? AccentDark { get; init; }

    /// <summary>
    /// Hide the color theme picker from the viewer toolbar.
    /// </summary>
    [Description("Hide the color theme picker from the viewer toolbar.")]
    [CommandOption("--no-theme-picker")]
    public bool NoThemePicker { get; init; }

}

/// <summary>
/// Shared Markdown-output settings for file and exec render commands.
/// </summary>
public abstract class MarkdownCommandSettingsBase : CommonCommandSettings
{
    /// <summary>
    /// Markdown layout mode. Supported values are <c>single</c>, <c>tree</c>, and <c>hybrid</c>.
    /// </summary>
    [Description("Markdown layout mode. Supported values are single, tree, and hybrid.")]
    [CommandOption("--layout <LAYOUT>")]
    public string? Layout { get; init; }

    /// <summary>
    /// Single Markdown file to write when using the <c>single</c> layout.
    /// </summary>
    [Description("Single Markdown file to write when using the single layout.")]
    [CommandOption("--out <FILE>")]
    public string? OutputFile { get; init; }

    /// <summary>
    /// Output directory to write when using the <c>tree</c> or <c>hybrid</c> layout.
    /// </summary>
    [Description("Output directory to write when using the tree or hybrid layout.")]
    [CommandOption("--out-dir <DIR>")]
    public string? OutputDirectory { get; init; }

    /// <summary>
    /// Depth at which the <c>hybrid</c> layout emits one Markdown file per command group.
    /// Defaults to 1 (one file per top-level group).
    /// </summary>
    [Description("Depth at which hybrid layout emits one file per command group (defaults to 1).")]
    [CommandOption("--split-depth <DEPTH>")]
    public int? SplitDepth { get; init; }
}

/// <summary>
/// Shared flags supported by the self-documentation command.
/// </summary>
public abstract class SelfDocCommandSettingsBase : CommandSettings
{
    /// <summary>
    /// Allow existing self-documentation output to be replaced.
    /// </summary>
    [Description("Allow existing self-documentation output to be replaced.")]
    [CommandOption("--overwrite")]
    public bool Overwrite { get; init; }

    /// <summary>
    /// Include hidden commands and options from InSpectra's own CLI surface.
    /// </summary>
    [Description("Include hidden commands and options from InSpectra's own CLI surface.")]
    [CommandOption("--include-hidden")]
    public bool IncludeHidden { get; init; }

    /// <summary>
    /// Include metadata sections in the generated Markdown tree and HTML bundle.
    /// </summary>
    [Description("Include metadata sections in the generated Markdown tree and HTML bundle.")]
    [CommandOption("--include-metadata")]
    public bool IncludeMetadata { get; init; }
}

/// <summary>
/// Shared HTML feature flags supported by the self-documentation command.
/// </summary>
public abstract class SelfDocHtmlCommandSettingsBase : SelfDocCommandSettingsBase
{
    /// <summary>
    /// Directory where the self-documentation bundle should be written.
    /// </summary>
    [Description("Directory where the self-documentation bundle should be written.")]
    [CommandOption("--out-dir <DIR>")]
    public string? OutputDirectory { get; init; }

    /// <summary>
    /// Show the viewer home screen button in the generated self-documentation app.
    /// </summary>
    [Description("Show the viewer home screen button in the generated self-documentation app.")]
    [CommandOption("--show-home")]
    public bool ShowHome { get; init; }

    /// <summary>
    /// Hide the interactive command composer from the generated self-documentation app.
    /// </summary>
    [Description("Hide the interactive command composer from the generated self-documentation app.")]
    [CommandOption("--no-composer")]
    public bool NoComposer { get; init; }

    /// <summary>
    /// Disable dark mode in the generated self-documentation app.
    /// </summary>
    [Description("Disable dark mode in the generated self-documentation app.")]
    [CommandOption("--no-dark")]
    public bool NoDark { get; init; }

    /// <summary>
    /// Disable light mode in the generated self-documentation app.
    /// </summary>
    [Description("Disable light mode in the generated self-documentation app.")]
    [CommandOption("--no-light")]
    public bool NoLight { get; init; }

    /// <summary>
    /// Allow the viewer to load alternate OpenCLI inputs from URL query parameters.
    /// </summary>
    [Description("Allow the viewer to load alternate OpenCLI inputs from URL query parameters.")]
    [CommandOption("--enable-url")]
    public bool EnableUrl { get; init; }

    /// <summary>
    /// Enable the NuGet package browser on the self-documentation viewer home screen.
    /// </summary>
    [Description("Enable the NuGet package browser on the self-documentation viewer home screen.")]
    [CommandOption("--enable-nuget-browser")]
    public bool EnableNugetBrowser { get; init; }

    /// <summary>
    /// Enable local package upload on the self-documentation viewer home screen.
    /// </summary>
    [Description("Enable local package upload on the self-documentation viewer home screen.")]
    [CommandOption("--enable-package-upload")]
    public bool EnablePackageUpload { get; init; }

    /// <summary>
    /// Custom label shown in the viewer header (e.g. a version string).
    /// </summary>
    [Description("Custom label shown in the viewer header (e.g. a version string).")]
    [CommandOption("--label <TEXT>")]
    public string? Label { get; init; }

    /// <summary>
    /// Override the CLI title shown in the viewer header and overview.
    /// </summary>
    [Description("Override the CLI title shown in the viewer header and overview.")]
    [CommandOption("--title <TEXT>")]
    public string? Title { get; init; }

    /// <summary>
    /// Override the CLI command prefix used in generated examples and the composer.
    /// </summary>
    [Description("Override the CLI command prefix used in generated examples and the composer.")]
    [CommandOption("--command-prefix <TEXT>")]
    public string? CommandPrefix { get; init; }

    /// <summary>
    /// Emit a single self-contained HTML file with all assets inlined. Works from file:// without a web server.
    /// </summary>
    [Description("Emit a single self-contained HTML file with all assets inlined. Works from file:// without a web server.")]
    [CommandOption("--single-file")]
    public bool SingleFile { get; init; }

    /// <summary>
    /// Compression level for the output: 0 = none, 1 = compress embedded JSON, 2 = self-extracting bundle (default 2).
    /// </summary>
    [Description("Compression level: 0 = none, 1 = compress embedded JSON, 2 = self-extracting bundle (default 2).")]
    [CommandOption("--compression-level <LEVEL>")]
    public int? CompressionLevel { get; init; }

    /// <summary>
    /// Set the initial theme mode (light or dark).
    /// </summary>
    [Description("Set the initial theme mode (light or dark).")]
    [CommandOption("--theme <MODE>")]
    public string? Theme { get; init; }

    /// <summary>
    /// Set the color theme (cyan, indigo, emerald, amber, rose, blue).
    /// </summary>
    [Description("Set the color theme (cyan, indigo, emerald, amber, rose, blue).")]
    [CommandOption("--color-theme <NAME>")]
    public string? ColorTheme { get; init; }

    /// <summary>
    /// Custom accent color for light mode (hex, e.g. "#7c3aed").
    /// </summary>
    [Description("Custom accent color for light mode (hex, e.g. \"#7c3aed\").")]
    [CommandOption("--accent <COLOR>")]
    public string? Accent { get; init; }

    /// <summary>
    /// Custom accent color for dark mode (hex). Falls back to --accent if omitted.
    /// </summary>
    [Description("Custom accent color for dark mode (hex). Falls back to --accent if omitted.")]
    [CommandOption("--accent-dark <COLOR>")]
    public string? AccentDark { get; init; }

    /// <summary>
    /// Hide the color theme picker from the viewer toolbar.
    /// </summary>
    [Description("Hide the color theme picker from the viewer toolbar.")]
    [CommandOption("--no-theme-picker")]
    public bool NoThemePicker { get; init; }

}
