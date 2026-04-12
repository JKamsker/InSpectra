namespace InSpectra.Lib.Rendering.Contracts;

public interface IHtmlRenderService
{
    Task<RenderExecutionResult> RenderFromFileAsync(
        FileRenderRequest request,
        HtmlFeatureFlags features,
        CancellationToken cancellationToken,
        string? label = null,
        string? title = null,
        string? commandPrefix = null,
        HtmlThemeOptions? themeOptions = null);
}
