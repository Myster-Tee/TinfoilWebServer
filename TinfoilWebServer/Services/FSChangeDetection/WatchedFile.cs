using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace TinfoilWebServer.Services.FSChangeDetection;

public class WatchedFile : WatchedPathFiltered, IWatchedFile
{
    private readonly ILogger<WatchedFile> _logger;

    public event FileChangedEventHandler? FileChanged;

    public bool FileChangedEventEnabled
    {
        get => _fileSystemWatcher.EnableRaisingEvents;
        set => _fileSystemWatcher.EnableRaisingEvents = value;
    }

    public string WatchedFilePath { get; }

    public WatchedFile(string filePath, bool enableChangeEvent, ILogger<WatchedFile> logger) : base(logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        WatchedFilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

        var fullFilePath = Path.GetFullPath(filePath);

        var fullDirPath = Path.GetDirectoryName(fullFilePath);
        if (fullDirPath == null)
            throw new ArgumentException($@"The directory of file to watch ""{filePath}"" can't be determined.", nameof(filePath));

        var fileName = Path.GetFileName(filePath);

        _fileSystemWatcher.Path = fullDirPath;
        _fileSystemWatcher.Filter = fileName;
        _fileSystemWatcher.IncludeSubdirectories = false;
        _fileSystemWatcher.EnableRaisingEvents = enableChangeEvent;
    }

    protected override void OnError(ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger.LogError(ex, $"An error occurred while watching changes of file \"{WatchedFilePath}\": {ex.Message}");
    }

    protected override void OnChange(FileSystemEventArgs e)
    {
        if (FileChangedEventEnabled)
            FileChanged?.Invoke(this, new FileChangedEventHandlerArgs(WatchedFilePath, e));
    }


}