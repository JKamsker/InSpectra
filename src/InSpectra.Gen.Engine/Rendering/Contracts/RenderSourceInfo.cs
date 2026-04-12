namespace InSpectra.Gen.Engine.Rendering.Contracts;

public sealed record RenderSourceInfo(
    string Kind,
    string OpenCliOrigin,
    string? XmlDocOrigin,
    string? ExecutablePath);
