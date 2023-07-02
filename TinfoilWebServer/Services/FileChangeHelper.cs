using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace TinfoilWebServer.Services;

public sealed class FileChangeHelper : IFileChangeHelper
{
    private readonly ILogger<FileChangeHelper> _logger;
    private FileSystemWatcher? _fileSystemWatcher;


    public FileChangeHelper(ILogger<FileChangeHelper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public event FileChangedEventHandler? FileChanged;

    public bool EnableFileChangedEvent { get; set; }

    public string? WatchedFileFullPath { get; private set; }

    public void WatchFile(string fullFilePath, bool enableFileChangedEvent)
    {
        if (fullFilePath == null)
            throw new ArgumentNullException(nameof(fullFilePath));

        if (!Path.IsPathRooted(fullFilePath))
            throw new ArgumentException($"The path of file to watch \"{fullFilePath}\" should be rooted.", nameof(fullFilePath));

        var directoryPath = Path.GetDirectoryName(fullFilePath);
        if (directoryPath == null)
            throw new ArgumentException($"The directory of file to watch \"{fullFilePath}\" can't be determined.", nameof(fullFilePath));

        StopWatching();

        try
        {
            WatchedFileFullPath = fullFilePath;

            _fileSystemWatcher = new FileSystemWatcher();
            _fileSystemWatcher.BeginInit();
            _fileSystemWatcher.Path = directoryPath;
            _fileSystemWatcher.Filter = Path.GetFileName(fullFilePath);
            _fileSystemWatcher.IncludeSubdirectories = false;
            _fileSystemWatcher.EnableRaisingEvents = true;
            _fileSystemWatcher.Changed += OnWatchedFileChanged;
            _fileSystemWatcher.Created += OnWatchedFileCreated;
            _fileSystemWatcher.Deleted += OnWatchedFileDeleted;
            _fileSystemWatcher.Renamed += OnWatchedFileRenamed;
            _fileSystemWatcher.Error += OnWatchedFileError;
            _fileSystemWatcher.EndInit();

            EnableFileChangedEvent = enableFileChangedEvent;
        }
        catch
        {
            StopWatching();
            throw;
        }
    }

    public bool StopWatching()
    {
        var wasWatching = _fileSystemWatcher != null;
        DisposeFileSystemWatcher(ref _fileSystemWatcher);
        WatchedFileFullPath = null;
        return wasWatching;
    }

    private void DisposeFileSystemWatcher(ref FileSystemWatcher? fileSystemWatcher)
    {
        if (fileSystemWatcher == null)
            return;

        try
        {
            fileSystemWatcher.Changed -= OnWatchedFileChanged;
            fileSystemWatcher.Created -= OnWatchedFileCreated;
            fileSystemWatcher.Deleted -= OnWatchedFileDeleted;
            fileSystemWatcher.Renamed -= OnWatchedFileRenamed;
            fileSystemWatcher.Error -= OnWatchedFileError;
            fileSystemWatcher.Dispose();
        }
        catch
        {
        }
        fileSystemWatcher = null;
    }

    private void OnWatchedFileError(object sender, ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger.LogError(ex, $"Change detection of file \"{WatchedFileFullPath}\" deactivated due to an unexpected failure: {ex.Message}");

        StopWatching();
    }

    private void OnWatchedFileRenamed(object sender, RenamedEventArgs e)
    {
        NotifyFileChanged();
    }

    private void OnWatchedFileDeleted(object sender, FileSystemEventArgs e)
    {
        NotifyFileChanged();
    }

    private void OnWatchedFileCreated(object sender, FileSystemEventArgs e)
    {
        NotifyFileChanged();
    }

    private void OnWatchedFileChanged(object sender, FileSystemEventArgs e)
    {
        NotifyFileChanged();
    }

    private void NotifyFileChanged()
    {
        if (EnableFileChangedEvent)
            FileChanged?.Invoke(this, new FileChangedEventHandlerArgs(this.WatchedFileFullPath!));
    }
}