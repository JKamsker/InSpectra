namespace InSpectra.Gen.Acquisition.Runtime;

public sealed class CliSourceExecutionException(string message, string errorKind = "source_exec", IReadOnlyList<string>? details = null, Exception? innerException = null)
    : CliException(message, errorKind, 3, details, innerException);
