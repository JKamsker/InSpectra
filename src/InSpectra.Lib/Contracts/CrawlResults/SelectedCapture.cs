namespace InSpectra.Lib.Contracts.CrawlResults;

using InSpectra.Lib.Contracts.Documents;


internal sealed record SelectedCapture(string CommandKey, Document Document);
