namespace InSpectra.Gen.Execution;

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken);

    Task<ProcessResult> RunAsync(
        string executablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken);
}
