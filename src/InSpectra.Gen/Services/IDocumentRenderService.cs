using InSpectra.Gen.Runtime;

namespace InSpectra.Gen.Services;

public interface IDocumentRenderService
{
    Task<AcquiredRenderDocument> LoadFromFileAsync(
        FileRenderRequest request,
        CancellationToken cancellationToken);
}
