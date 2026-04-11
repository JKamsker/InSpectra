namespace InSpectra.Gen.Acquisition.Contracts.Exceptions;

public sealed class CliUsageException(string message, IReadOnlyList<string>? details = null)
    : CliException(message, "usage", 2, details);
