using InSpectra.Gen.Runtime.Rendering;

namespace InSpectra.Gen.Rendering;

public interface IDocumentRenderService
{
    Task<AcquiredRenderDocument> LoadFromFileAsync(
        FileRenderRequest request,
        CancellationToken cancellationToken);
}
