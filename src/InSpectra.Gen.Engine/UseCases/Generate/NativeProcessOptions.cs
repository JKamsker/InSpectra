namespace InSpectra.Gen.Engine.UseCases.Generate;

internal sealed record NativeProcessOptions(
    string ExecutablePath,
    IReadOnlyList<string> SourceArguments,
    IReadOnlyList<string> OpenCliArguments,
    bool IncludeXmlDoc,
    IReadOnlyList<string> XmlDocArguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string>? Environment,
    string? CleanupRoot,
    int TimeoutSeconds);
