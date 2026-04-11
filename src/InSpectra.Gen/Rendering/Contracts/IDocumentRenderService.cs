using InSpectra.Gen.Rendering.Pipeline;

namespace InSpectra.Gen.Rendering.Contracts;

public interface IDocumentRenderService
{
    Task<AcquiredRenderDocument> LoadFromFileAsync(
        FileRenderRequest request,
        CancellationToken cancellationToken);
}
