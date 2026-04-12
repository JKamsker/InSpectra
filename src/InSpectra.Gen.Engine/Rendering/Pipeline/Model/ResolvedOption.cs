using InSpectra.Gen.Engine.OpenCli.Model;

namespace InSpectra.Gen.Engine.Rendering.Pipeline.Model;

internal sealed class ResolvedOption
{
    public required OpenCliOption Option { get; init; }

    public required bool IsInherited { get; init; }

    public string? InheritedFromPath { get; init; }
}
