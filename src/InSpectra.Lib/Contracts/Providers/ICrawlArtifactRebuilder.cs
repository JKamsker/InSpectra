using System.Text.Json.Nodes;

namespace InSpectra.Lib.Contracts.Providers;

/// <summary>
/// Rebuilds an OpenCLI document from a stored crawl capture artifact.
/// </summary>
public interface ICrawlArtifactRebuilder
{
    /// <summary>
    /// Parses a crawl capture JSON object and rebuilds an OpenCLI document from
    /// the stored help output. Returns null if no valid documents could be parsed.
    /// </summary>
    /// <param name="crawlJson">The crawl capture JSON (containing a "commands" array).</param>
    /// <param name="commandName">The root command name of the analyzed tool.</param>
    /// <param name="version">The package version.</param>
    /// <param name="cliFramework">Optional CLI framework to embed in metadata.</param>
    JsonObject? RebuildOpenCli(JsonObject crawlJson, string commandName, string version, string? cliFramework = null);
}
