using System.ComponentModel;
using InSpectra.Gen.Commands.Common;
using Spectre.Console.Cli;

namespace InSpectra.Gen.Commands.Render;

/// <summary>
/// Settings for rendering Markdown from saved OpenCLI export files.
/// </summary>
public sealed class FileRenderSettings : MarkdownCommandSettingsBase
{
    /// <summary>
    /// Path to the OpenCLI JSON export file to render.
    /// </summary>
    [Description("Path to the OpenCLI JSON export file to render.")]
    [CommandArgument(0, "<OPENCLI_JSON>")]
    public string OpenCliJsonPath { get; init; } = string.Empty;

    /// <summary>
    /// Optional XML documentation file used to enrich missing descriptions.
    /// </summary>
    [Description("Optional XML documentation file used to enrich missing descriptions.")]
    [CommandOption("--xmldoc <PATH>")]
    public string? XmlDocPath { get; init; }
}
