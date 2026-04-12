namespace InSpectra.Gen.Core;

public sealed class CliUsageException(string message, IReadOnlyList<string>? details = null)
    : CliException(message, "usage", 2, details);
