namespace InSpectra.Gen.Rendering.Html.Bundle;

public interface IViewerBundleLocator
{
    Task<string> ResolveAsync(CancellationToken cancellationToken, bool allowBuild = true);
}
