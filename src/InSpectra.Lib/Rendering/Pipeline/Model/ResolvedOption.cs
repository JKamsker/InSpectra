using InSpectra.Lib.OpenCli.Model;

namespace InSpectra.Lib.Rendering.Pipeline.Model;

internal sealed class ResolvedOption
{
    public required OpenCliOption Option { get; init; }

    public required bool IsInherited { get; init; }

    public string? InheritedFromPath { get; init; }
}
