namespace InSpectra.Gen.Acquisition.Contracts.Signatures;


internal sealed record OptionSignature(
    string? PrimaryName,
    IReadOnlyList<string> Aliases,
    string? ArgumentName,
    bool ArgumentRequired);
