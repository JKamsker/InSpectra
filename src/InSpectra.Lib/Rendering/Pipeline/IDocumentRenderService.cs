using InSpectra.Lib.Rendering.Contracts;

namespace InSpectra.Lib.Rendering.Pipeline;

internal interface IDocumentRenderService
{
    Task<AcquiredRenderDocument> LoadFromFileAsync(
        FileRenderRequest request,
        CancellationToken cancellationToken);
}
