using System.Text.Json;
using System.Text.Json.Serialization;

namespace InSpectra.Gen.Runtime;

public enum ResolvedOutputMode
{
    Human,
    Json,
}

public enum DocumentFormat
{
    Markdown,
    Html,
}

public enum RenderLayout
{
    Single,
    Tree,
    Hybrid,
    App,
}

public sealed record MarkdownRenderOptions(
    int HybridSplitDepth,
    string? Title = null,
    string? CommandPrefix = null);

public sealed record RenderExecutionOptions(
    RenderLayout Layout,
    ResolvedOutputMode OutputMode,
    bool DryRun,
    bool Quiet,
    bool Verbose,
    bool NoColor,
    bool IncludeHidden,
    bool IncludeMetadata,
    bool Overwrite,
    bool SingleFile,
    int CompressLevel,
    string? OutputFile,
    string? OutputDirectory);

public sealed record HtmlFeatureFlags(
    bool ShowHome,
    bool Composer,
    bool DarkTheme,
    bool LightTheme,
    bool UrlLoading,
    bool NugetBrowser,
    bool PackageUpload,
    bool ColorThemePicker);

public sealed record HtmlThemeOptions(
    string? Theme,
    string? ColorTheme,
    string? CustomAccent,
    string? CustomAccentDark);

public sealed record FileRenderRequest(
    string OpenCliJsonPath,
    string? XmlDocPath,
    RenderExecutionOptions Options,
    MarkdownRenderOptions? MarkdownOptions = null);

public sealed record ExecRenderRequest(
    string Source,
    IReadOnlyList<string> SourceArguments,
    IReadOnlyList<string> OpenCliArguments,
    bool IncludeXmlDoc,
    IReadOnlyList<string> XmlDocArguments,
    string WorkingDirectory,
    int TimeoutSeconds,
    RenderExecutionOptions Options,
    MarkdownRenderOptions? MarkdownOptions = null);

public sealed record RenderSourceInfo(
    string Kind,
    string OpenCliOrigin,
    string? XmlDocOrigin,
    string? ExecutablePath);

public sealed record RenderStats(
    int CommandCount,
    int OptionCount,
    int ArgumentCount,
    int FileCount);

public sealed record RenderedFile(
    string RelativePath,
    string FullPath,
    string? Content);

public sealed class RenderExecutionResult
{
    public required DocumentFormat Format { get; init; }

    public required RenderLayout Layout { get; init; }

    public required RenderSourceInfo Source { get; init; }

    public required RenderStats Stats { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }

    public required bool IsDryRun { get; init; }

    public string? StdoutDocument { get; init; }

    public required IReadOnlyList<RenderedFile> Files { get; init; }

    public string? Summary { get; init; }
}

public sealed class JsonEnvelope<T>
{
    [JsonPropertyName("ok")]
    public required bool Ok { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("error")]
    public JsonError? Error { get; init; }

    [JsonPropertyName("meta")]
    public required JsonMeta Meta { get; init; }
}

public sealed class JsonMeta
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class JsonError
{
    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("details")]
    public IReadOnlyList<string> Details { get; init; } = [];
}

public static class JsonOutput
{
    public static JsonSerializerOptions SerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static JsonSerializerOptions CompactSerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };
}
