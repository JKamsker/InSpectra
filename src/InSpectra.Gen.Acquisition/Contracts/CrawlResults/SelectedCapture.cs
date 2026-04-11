namespace InSpectra.Gen.Acquisition.Contracts.CrawlResults;

using InSpectra.Gen.Acquisition.Contracts.Documents;


internal sealed record SelectedCapture(string CommandKey, Document Document);
