namespace InSpectra.Gen.Core;

public class CliException(string message, string errorKind, int exitCode, IReadOnlyList<string>? details = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string ErrorKind { get; } = errorKind;

    public int ExitCode { get; } = exitCode;

    public IReadOnlyList<string> Details { get; } = details ?? [];
}
