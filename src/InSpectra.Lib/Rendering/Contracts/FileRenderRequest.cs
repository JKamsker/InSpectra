namespace InSpectra.Lib.Rendering.Contracts;

public sealed record FileRenderRequest(
    string OpenCliJsonPath,
    string? XmlDocPath,
    RenderExecutionOptions Options,
    MarkdownRenderOptions? MarkdownOptions = null);
