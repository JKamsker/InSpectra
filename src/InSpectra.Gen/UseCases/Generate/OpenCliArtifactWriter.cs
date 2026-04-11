using InSpectra.Gen.Core;
using InSpectra.Gen.UseCases.Generate.Requests;

namespace InSpectra.Gen.UseCases.Generate;

internal static class OpenCliArtifactWriter
{
    public static async Task<OpenCliArtifactOptions> WriteArtifactsAsync(
        OpenCliArtifactOptions requested,
        string openCliJson,
        string? crawlJson,
        CancellationToken cancellationToken)
    {
        var openCliArtifact = PrepareArtifact(requested.OpenCliOutputPath, openCliJson, requested.Overwrite);
        var crawlArtifact = PrepareArtifact(requested.CrawlOutputPath, crawlJson, requested.Overwrite);

        StagedArtifact? stagedOpenCli = null;
        StagedArtifact? stagedCrawl = null;
        CommittedArtifact? committedOpenCli = null;
        CommittedArtifact? committedCrawl = null;
        var commitCompleted = false;
        try
        {
            stagedOpenCli = await StageArtifactAsync(openCliArtifact, cancellationToken);
            stagedCrawl = await StageArtifactAsync(crawlArtifact, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            committedOpenCli = CommitStagedArtifact(stagedOpenCli);
            stagedOpenCli = null;
            committedCrawl = CommitStagedArtifact(stagedCrawl);
            stagedCrawl = null;

            var openCliPath = committedOpenCli?.Path;
            var crawlPath = committedCrawl?.Path;
            commitCompleted = true;
            return new OpenCliArtifactOptions(openCliPath, crawlPath);
        }
        catch
        {
            if (!commitCompleted)
            {
                RollBackCommittedArtifact(committedCrawl);
                RollBackCommittedArtifact(committedOpenCli);
            }

            throw;
        }
        finally
        {
            DeleteStagedArtifact(stagedOpenCli);
            DeleteStagedArtifact(stagedCrawl);
            TryDeleteBackupArtifact(committedOpenCli);
            TryDeleteBackupArtifact(committedCrawl);
        }
    }

    private static PreparedArtifact? PrepareArtifact(
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

        return new PreparedArtifact(fullPath, content, overwrite);
    }

    private static async Task<StagedArtifact?> StageArtifactAsync(PreparedArtifact? artifact, CancellationToken cancellationToken)
    {
        if (artifact is null)
        {
            return null;
        }

        var stagedArtifact = new StagedArtifact(artifact.Path, artifact.Path + $".{Guid.NewGuid():N}.tmp", artifact.Overwrite);
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

    private sealed record PreparedArtifact(string Path, string Content, bool Overwrite);

    private static CommittedArtifact? CommitStagedArtifact(StagedArtifact? artifact)
    {
        if (artifact is null)
        {
            return null;
        }

        string? backupPath = null;
        if (artifact.Overwrite && File.Exists(artifact.Path))
        {
            backupPath = artifact.Path + $".{Guid.NewGuid():N}.bak";
            File.Move(artifact.Path, backupPath, overwrite: false);
        }

        try
        {
            File.Move(artifact.TempPath, artifact.Path, overwrite: false);
            return new CommittedArtifact(artifact.Path, backupPath);
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

    private sealed record StagedArtifact(string Path, string TempPath, bool Overwrite);

    private sealed record CommittedArtifact(string Path, string? BackupPath);
}
