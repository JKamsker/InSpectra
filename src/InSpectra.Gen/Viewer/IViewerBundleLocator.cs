namespace InSpectra.Gen.Viewer;

public interface IViewerBundleLocator
{
    Task<string> ResolveAsync(CancellationToken cancellationToken, bool allowBuild = true);
}
