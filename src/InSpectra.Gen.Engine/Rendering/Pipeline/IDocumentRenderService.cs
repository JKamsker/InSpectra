using InSpectra.Gen.Engine.Rendering.Contracts;

namespace InSpectra.Gen.Engine.Rendering.Pipeline;

internal interface IDocumentRenderService
{
    Task<AcquiredRenderDocument> LoadFromFileAsync(
        FileRenderRequest request,
        CancellationToken cancellationToken);
}
