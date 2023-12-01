using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace TinfoilWebServer.Services.FSChangeDetection;

public class WatchedDirectory : WatchedPathFiltered, IWatchedDirectory
{

    private readonly ILogger<WatchedDirectory> _logger;

    public event DirectoryChangedEventHandler? DirectoryChanged;

    public bool DirectoryChangedEventEnabled
    {
        get => _fileSystemWatcher.EnableRaisingEvents;
        set => _fileSystemWatcher.EnableRaisingEvents = value;
    }

    public string WatchedDirectoryPath { get; }

    public WatchedDirectory(string directoryPath, bool enableChangeEvent, ILogger<WatchedDirectory> logger) : base(logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        WatchedDirectoryPath = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));

        var fullDirPath = Path.GetFullPath(directoryPath);
        if (fullDirPath == null)
            throw new ArgumentException($@"The directory of file to watch ""{directoryPath}"" can't be determined.", nameof(directoryPath));

        _fileSystemWatcher.Path = directoryPath;
        _fileSystemWatcher.Filter = "*";
        _fileSystemWatcher.IncludeSubdirectories = true;
        _fileSystemWatcher.EnableRaisingEvents = enableChangeEvent;
    }

    protected override void OnError(ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger.LogError(ex, $"An error occurred while watching changes of directory \"{WatchedDirectoryPath}\": {ex.Message}");
    }


    protected override void OnChange(FileSystemEventArgs e)
    {
        if (DirectoryChangedEventEnabled)
            DirectoryChanged?.Invoke(this, new DirectoryChangedEventHandlerArgs(WatchedDirectoryPath, e));
    }


}