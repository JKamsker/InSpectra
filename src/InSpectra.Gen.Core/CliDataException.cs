namespace InSpectra.Gen.Core;

public sealed class CliDataException(string message, IReadOnlyList<string>? details = null, Exception? innerException = null)
    : CliException(message, "validation", 4, details, innerException);
