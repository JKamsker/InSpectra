namespace InSpectra.Gen.Engine.Contracts.CrawlResults;

using InSpectra.Gen.Engine.Contracts.Documents;


internal sealed record SelectedCapture(string CommandKey, Document Document);
