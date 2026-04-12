namespace InSpectra.Lib.Rendering.Contracts;

public interface IMarkdownRenderService
{
    Task<RenderExecutionResult> RenderFromFileAsync(
        FileRenderRequest request,
        CancellationToken cancellationToken);
}
