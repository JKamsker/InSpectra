namespace InSpectra.Gen.Engine.Rendering.Html.Bundle;

internal interface IViewerBundleLocator
{
    Task<string> ResolveAsync(CancellationToken cancellationToken, bool allowBuild = true);
}
