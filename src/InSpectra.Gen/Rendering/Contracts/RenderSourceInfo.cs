namespace InSpectra.Gen.Rendering.Contracts;

public sealed record RenderSourceInfo(
    string Kind,
    string OpenCliOrigin,
    string? XmlDocOrigin,
    string? ExecutablePath);
