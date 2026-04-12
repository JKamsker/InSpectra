namespace InSpectra.Gen.Engine.Rendering.Contracts;

public interface IMarkdownRenderService
{
    Task<RenderExecutionResult> RenderFromFileAsync(
        FileRenderRequest request,
        CancellationToken cancellationToken);
}
