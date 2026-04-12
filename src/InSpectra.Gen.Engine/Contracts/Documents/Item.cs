namespace InSpectra.Gen.Engine.Contracts.Documents;


internal sealed record Item(
    string Key,
    bool IsRequired,
    string? Description);
