namespace InSpectra.Gen.Runtime;

public class CliException(string message, string errorKind, int exitCode, IReadOnlyList<string>? details = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string ErrorKind { get; } = errorKind;

    public int ExitCode { get; } = exitCode;

    public IReadOnlyList<string> Details { get; } = details ?? [];
}

public sealed class CliUsageException(string message, IReadOnlyList<string>? details = null)
    : CliException(message, "usage", 2, details);

public sealed class CliSourceExecutionException(string message, string errorKind = "source_exec", IReadOnlyList<string>? details = null, Exception? innerException = null)
    : CliException(message, errorKind, 3, details, innerException);

public sealed class CliDataException(string message, IReadOnlyList<string>? details = null, Exception? innerException = null)
    : CliException(message, "validation", 4, details, innerException);
