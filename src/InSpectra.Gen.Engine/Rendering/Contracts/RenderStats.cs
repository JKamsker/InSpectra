namespace InSpectra.Gen.Engine.Rendering.Contracts;

public sealed record RenderStats(
    int CommandCount,
    int OptionCount,
    int ArgumentCount,
    int FileCount);
