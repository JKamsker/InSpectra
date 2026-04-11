namespace InSpectra.Gen.Acquisition.Contracts.Documents;


internal sealed record Item(
    string Key,
    bool IsRequired,
    string? Description);
