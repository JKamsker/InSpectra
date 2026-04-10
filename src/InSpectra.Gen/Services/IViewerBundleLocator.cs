namespace InSpectra.Gen.Services;

public interface IViewerBundleLocator
{
    Task<string> ResolveAsync(CancellationToken cancellationToken);
}
