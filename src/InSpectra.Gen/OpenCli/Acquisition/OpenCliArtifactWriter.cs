using InSpectra.Gen.Acquisition.Contracts.Exceptions;
using InSpectra.Gen.Runtime.Acquisition;
using InSpectra.Gen.Rendering;

namespace InSpectra.Gen.OpenCli.Acquisition;

internal static class OpenCliArtifactWriter
{
    public static OpenCliArtifactOptions WriteArtifacts(
        OpenCliArtifactOptions requested,
        string openCliJson,
        string? crawlJson)
    {
        var openCliPath = WriteArtifact(requested.OpenCliOutputPath, openCliJson, requested.Overwrite);
        var crawlPath = WriteArtifact(requested.CrawlOutputPath, crawlJson, requested.Overwrite);
        return new OpenCliArtifactOptions(openCliPath, crawlPath);
    }

    private static string? WriteArtifact(string? path, string? content, bool overwrite)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (content is null)
        {
            return null;
        }

        var fullPath = Path.GetFullPath(path);
        OutputPathHelper.EnsureFileWritable(fullPath, overwrite);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);
        return fullPath;
    }
}
