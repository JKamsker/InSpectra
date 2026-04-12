namespace InSpectra.Gen.Engine.Modes.Hook.Execution;


internal sealed record HookToolProcessInvocation(
    string FilePath,
    IReadOnlyList<string> ArgumentList,
    string? PreferredAssemblyPath);
