using InSpectra.Gen.Rendering.Contracts;

namespace InSpectra.Gen.Rendering;

public interface IDocumentRenderService
{
    Task<AcquiredRenderDocument> LoadFromFileAsync(
        FileRenderRequest request,
        CancellationToken cancellationToken);
}
