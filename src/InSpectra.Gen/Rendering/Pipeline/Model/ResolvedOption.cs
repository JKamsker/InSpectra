namespace InSpectra.Gen.Rendering.Pipeline.Model;

public sealed class ResolvedOption
{
    public required OpenCliOption Option { get; init; }

    public required bool IsInherited { get; init; }

    public string? InheritedFromPath { get; init; }
}
