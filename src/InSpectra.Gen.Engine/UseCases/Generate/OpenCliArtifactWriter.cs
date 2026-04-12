using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Rendering.Pipeline;
using InSpectra.Gen.Engine.UseCases.Generate.Requests;

namespace InSpectra.Gen.Engine.UseCases.Generate;

internal static class OpenCliArtifactWriter
{
    public static async Task<OpenCliArtifactOptions> WriteArtifactsAsync(
        OpenCliArtifactOptions requested,
        string openCliJson,
        string? crawlJson,
        CancellationToken cancellationToken)
    {
        var publishedArtifacts = await PublishArtifactsAsync(
            [
                PrepareArtifact("opencli", requested.OpenCliOutputPath, openCliJson, requested.Overwrite),
                PrepareArtifact("crawl", requested.CrawlOutputPath, crawlJson, requested.Overwrite),
            ],
            cancellationToken);

        return new OpenCliArtifactOptions(
            GetPublishedPath(publishedArtifacts, "opencli"),
            GetPublishedPath(publishedArtifacts, "crawl"));
    }

    public static async Task<GenerateArtifactPublicationResult> WriteGenerateArtifactsAsync(
        string? outputFile,
        bool outputOverwrite,
        OpenCliArtifactOptions requestedArtifacts,
        string outputJson,
        string openCliArtifactJson,
        string? crawlJson,
        CancellationToken cancellationToken)
    {
        var outputArtifact = PrepareArtifact("output", outputFile, outputJson, outputOverwrite);
        var openCliArtifact = PrepareArtifact("opencli", requestedArtifacts.OpenCliOutputPath, openCliArtifactJson, requestedArtifacts.Overwrite);
        var crawlArtifact = PrepareArtifact("crawl", requestedArtifacts.CrawlOutputPath, crawlJson, requestedArtifacts.Overwrite);

        var openCliSharesOutput = outputArtifact is not null
            && openCliArtifact is not null
            && string.Equals(outputArtifact.Path, openCliArtifact.Path, StringComparison.OrdinalIgnoreCase);
        if (openCliSharesOutput)
        {
            OutputPathHelper.EnsureFileWritable(outputArtifact!.Path, requestedArtifacts.Overwrite);
            openCliArtifact = null;
        }

        var publishedArtifacts = await PublishArtifactsAsync(
            [
                outputArtifact,
                openCliArtifact,
                crawlArtifact,
            ],
            cancellationToken);

        var publishedOutputPath = GetPublishedPath(publishedArtifacts, "output");
        return new GenerateArtifactPublicationResult(
            publishedOutputPath,
            openCliSharesOutput ? publishedOutputPath : GetPublishedPath(publishedArtifacts, "opencli"),
            GetPublishedPath(publishedArtifacts, "crawl"));
    }

    private static PreparedArtifact? PrepareArtifact(
        string key,
        string? path,
        string? content,
        bool overwrite)
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

        return new PreparedArtifact(key, fullPath, content, overwrite);
    }

    private static async Task<IReadOnlyDictionary<string, string>> PublishArtifactsAsync(
        IEnumerable<PreparedArtifact?> artifacts,
        CancellationToken cancellationToken)
    {
        var preparedArtifacts = artifacts
            .Where(artifact => artifact is not null)
            .Cast<PreparedArtifact>()
            .ToList();
        var stagedArtifacts = new List<StagedArtifact>(preparedArtifacts.Count);
        var committedArtifacts = new List<CommittedArtifact>(preparedArtifacts.Count);
        var commitCompleted = false;
        try
        {
            foreach (var artifact in preparedArtifacts)
            {
                stagedArtifacts.Add(await StageArtifactAsync(artifact, cancellationToken));
            }

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var stagedArtifact in stagedArtifacts)
            {
                committedArtifacts.Add(CommitStagedArtifact(stagedArtifact));
            }

            commitCompleted = true;
            return committedArtifacts.ToDictionary(artifact => artifact.Key, artifact => artifact.Path, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            if (!commitCompleted)
            {
                for (var i = committedArtifacts.Count - 1; i >= 0; i--)
                {
                    RollBackCommittedArtifact(committedArtifacts[i]);
                }
            }

            throw;
        }
        finally
        {
            foreach (var stagedArtifact in stagedArtifacts)
            {
                DeleteStagedArtifact(stagedArtifact);
            }

            foreach (var committedArtifact in committedArtifacts)
            {
                TryDeleteBackupArtifact(committedArtifact);
            }
        }
    }

    private static string? GetPublishedPath(
        IReadOnlyDictionary<string, string> publishedArtifacts,
        string key)
    {
        return publishedArtifacts.TryGetValue(key, out var path)
            ? path
            : null;
    }

    private static async Task<StagedArtifact> StageArtifactAsync(PreparedArtifact artifact, CancellationToken cancellationToken)
    {
        var stagedArtifact = new StagedArtifact(artifact.Key, artifact.Path, artifact.Path + $".{Guid.NewGuid():N}.tmp", artifact.Overwrite);
        try
        {
            await File.WriteAllTextAsync(stagedArtifact.TempPath, artifact.Content, cancellationToken);
            return stagedArtifact;
        }
        catch
        {
            DeleteStagedArtifact(stagedArtifact);
            throw;
        }
    }

    private sealed record PreparedArtifact(string Key, string Path, string Content, bool Overwrite);

    private static CommittedArtifact CommitStagedArtifact(StagedArtifact artifact)
    {
        string? backupPath = null;
        if (artifact.Overwrite && File.Exists(artifact.Path))
        {
            backupPath = artifact.Path + $".{Guid.NewGuid():N}.bak";
            File.Move(artifact.Path, backupPath, overwrite: false);
        }

        try
        {
            File.Move(artifact.TempPath, artifact.Path, overwrite: false);
            return new CommittedArtifact(artifact.Key, artifact.Path, backupPath);
        }
        catch
        {
            RestoreBackupArtifact(artifact.Path, backupPath);
            throw;
        }
    }

    private static void DeleteStagedArtifact(StagedArtifact? artifact)
    {
        if (artifact is null || !File.Exists(artifact.TempPath))
        {
            return;
        }

        File.Delete(artifact.TempPath);
    }

    private static void RollBackCommittedArtifact(CommittedArtifact? artifact)
    {
        if (artifact is null)
        {
            return;
        }

        if (File.Exists(artifact.Path))
        {
            File.Delete(artifact.Path);
        }

        RestoreBackupArtifact(artifact.Path, artifact.BackupPath);
    }

    private static void TryDeleteBackupArtifact(CommittedArtifact? artifact)
    {
        if (artifact is null || string.IsNullOrWhiteSpace(artifact.BackupPath) || !File.Exists(artifact.BackupPath))
        {
            return;
        }

        try
        {
            File.Delete(artifact.BackupPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void RestoreBackupArtifact(string path, string? backupPath)
    {
        if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
        {
            return;
        }

        File.Move(backupPath, path, overwrite: false);
    }

    private sealed record StagedArtifact(string Key, string Path, string TempPath, bool Overwrite);

    private sealed record CommittedArtifact(string Key, string Path, string? BackupPath);
}

internal sealed record GenerateArtifactPublicationResult(
    string? OutputFile,
    string? OpenCliOutputPath,
    string? CrawlOutputPath);
