using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace TinfoilWebServer.Services;

public sealed class FileChangeHelper : IFileChangeHelper
{
    private readonly ILogger<WatchedFile> _logger;


    public FileChangeHelper(ILogger<WatchedFile> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IWatchedFile WatchFile(string filePath, bool enableFileChangedEvent = true)
    {
        return new WatchedFile(filePath, enableFileChangedEvent, _logger);
    }

}

public class WatchedFile : IWatchedFile
{
    private readonly ILogger<WatchedFile> _logger;
    private readonly FileSystemWatcher _fileSystemWatcher;

    public event FileChangedEventHandler? FileChanged;

    public bool FileChangedEventEnabled
    {
        get => _fileSystemWatcher.EnableRaisingEvents;
        set => _fileSystemWatcher.EnableRaisingEvents = value;
    }

    public string WatchedFilePath { get; }

    public WatchedFile(string filePath, bool enableFileChangedEvent, ILogger<WatchedFile> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));

        var fullFilePath = Path.GetFullPath(filePath);

        var fullDirPath = Path.GetDirectoryName(fullFilePath);
        if (fullDirPath == null)
            throw new ArgumentException($@"The directory of file to watch ""{filePath}"" can't be determined.", nameof(filePath));

        var fileName = Path.GetFileName(filePath);

        WatchedFilePath = filePath;

        try
        {
            _fileSystemWatcher = new FileSystemWatcher(fullDirPath, fileName);
            _fileSystemWatcher.IncludeSubdirectories = false;
            _fileSystemWatcher.Changed += OnWatchedFileChanged;
            _fileSystemWatcher.Created += OnWatchedFileCreated;
            _fileSystemWatcher.Deleted += OnWatchedFileDeleted;
            _fileSystemWatcher.Renamed += OnWatchedFileRenamed;
            _fileSystemWatcher.Error += OnWatchedFileError;
            _fileSystemWatcher.EnableRaisingEvents = enableFileChangedEvent;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _fileSystemWatcher.Changed -= OnWatchedFileChanged;
            _fileSystemWatcher.Created -= OnWatchedFileCreated;
            _fileSystemWatcher.Deleted -= OnWatchedFileDeleted;
            _fileSystemWatcher.Renamed -= OnWatchedFileRenamed;
            _fileSystemWatcher.Error -= OnWatchedFileError;
            _fileSystemWatcher.Dispose();
        }
        catch
        {
            //ignore
        }
    }

    private void OnWatchedFileError(object sender, ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger.LogError(ex, $"An error occurred while watching changes of file \"{WatchedFilePath}\": {ex.Message}");
    }

    private void OnWatchedFileRenamed(object sender, RenamedEventArgs e)
    {
        NotifyFileChanged(e);
    }

    private void OnWatchedFileDeleted(object sender, FileSystemEventArgs e)
    {
        NotifyFileChanged(e);
    }

    private void OnWatchedFileCreated(object sender, FileSystemEventArgs e)
    {
        NotifyFileChanged(e);
    }

    private void OnWatchedFileChanged(object sender, FileSystemEventArgs e)
    {
        NotifyFileChanged(e);
    }

    private void NotifyFileChanged(FileSystemEventArgs e)
    {
        if (FileChangedEventEnabled)
            FileChanged?.Invoke(this, new FileChangedEventHandlerArgs(this.WatchedFilePath, e));
    }

}