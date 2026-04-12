using InSpectra.Gen.Core;

namespace InSpectra.Gen.Engine.Rendering.Pipeline;

internal sealed class OutputPathFilePublication : IDisposable
{
    private readonly string _targetPath;
    private readonly string _tempPath;
    private readonly bool _overwrite;
    private string? _backupPath;
    private bool _committed;

    public OutputPathFilePublication(string targetPath, string tempPath, bool overwrite)
    {
        _targetPath = targetPath;
        _tempPath = tempPath;
        _overwrite = overwrite;
    }

    public string TempPath => _tempPath;

    public void Commit()
    {
        if (_overwrite && File.Exists(_targetPath))
        {
            _backupPath = _targetPath + $".{Guid.NewGuid():N}.bak";
            File.Move(_targetPath, _backupPath, overwrite: false);
        }

        try
        {
            File.Move(_tempPath, _targetPath, overwrite: false);
            _committed = true;
        }
        catch
        {
            RestoreBackup();
            throw;
        }
    }

    public void Dispose()
    {
        if (File.Exists(_tempPath))
        {
            File.Delete(_tempPath);
        }

        if (_committed && !string.IsNullOrWhiteSpace(_backupPath) && File.Exists(_backupPath))
        {
            OutputPathCleanupSupport.TryDeleteFile(_backupPath);
        }
    }

    private void RestoreBackup()
    {
        if (string.IsNullOrWhiteSpace(_backupPath) || !File.Exists(_backupPath))
        {
            return;
        }

        File.Move(_backupPath, _targetPath, overwrite: false);
    }
}

internal sealed class OutputPathDirectoryPublication : IDisposable
{
    private readonly string _targetPath;
    private readonly string _stagingPath;
    private readonly bool _overwrite;
    private readonly bool _targetExists;
    private string? _backupPath;
    private bool _committed;

    public OutputPathDirectoryPublication(string targetPath, string stagingPath, bool overwrite, bool targetExists)
    {
        _targetPath = targetPath;
        _stagingPath = stagingPath;
        _overwrite = overwrite;
        _targetExists = targetExists;
    }

    public string StagingPath => _stagingPath;

    public void Commit()
    {
        if (_targetExists && Directory.Exists(_targetPath))
        {
            var hasEntries = Directory.EnumerateFileSystemEntries(_targetPath).Any();
            if (hasEntries && !_overwrite)
            {
                throw new CliUsageException($"Output directory `{_targetPath}` is not empty. Enable overwrite to replace it.");
            }

            _backupPath = _targetPath + $".{Guid.NewGuid():N}.bak";
            Directory.Move(_targetPath, _backupPath);
        }

        try
        {
            Directory.Move(_stagingPath, _targetPath);
            _committed = true;
        }
        catch
        {
            RestoreBackup();
            throw;
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_stagingPath))
        {
            Directory.Delete(_stagingPath, recursive: true);
        }

        if (_committed && !string.IsNullOrWhiteSpace(_backupPath) && Directory.Exists(_backupPath))
        {
            OutputPathCleanupSupport.TryDeleteDirectory(_backupPath);
        }
    }

    private void RestoreBackup()
    {
        if (string.IsNullOrWhiteSpace(_backupPath) || !Directory.Exists(_backupPath) || Directory.Exists(_targetPath))
        {
            return;
        }

        Directory.Move(_backupPath, _targetPath);
    }
}

internal static class OutputPathCleanupSupport
{
    public static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
