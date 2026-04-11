namespace InSpectra.Gen.Acquisition.Modes.Hook.Execution;


internal sealed record HookToolProcessInvocationResolution(
    HookToolProcessInvocation? Invocation,
    string? TerminalFailureClassification,
    string? TerminalFailureMessage)
{
    public static HookToolProcessInvocationResolution FromInvocation(HookToolProcessInvocation invocation)
        => new(invocation, null, null);

    public static HookToolProcessInvocationResolution TerminalFailure(string classification, string message)
        => new(null, classification, message);
}
