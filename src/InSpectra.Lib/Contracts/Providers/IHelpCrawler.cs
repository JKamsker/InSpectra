namespace InSpectra.Lib.Contracts.Providers;

using InSpectra.Lib.Contracts.CrawlResults;

/// <summary>
/// Abstraction over the help-crawl engine used by non-Help modes (Static, CliFx)
/// that need to invoke a generic help crawl without depending on Help-mode internals.
/// The concrete implementation lives in <c>Modes/Help/Crawling/Crawler.cs</c>.
/// </summary>
internal interface IHelpCrawler
{
    Task<CrawlResult> CrawlAsync(
        string commandPath,
        string rootCommandName,
        string workingDirectory,
        IReadOnlyDictionary<string, string> environment,
        int timeoutSeconds,
        string? sandboxCleanupRoot,
        CancellationToken cancellationToken);
}
