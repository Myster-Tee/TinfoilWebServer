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

    public DirectoryInfo Directory { get; }

    public WatchedDirectory(DirectoryInfo directory, bool enableChangeEvent, ILogger<WatchedDirectory> logger) : base(logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Directory = directory ?? throw new ArgumentNullException(nameof(directory));

        _fileSystemWatcher.Path = directory.FullName;
        _fileSystemWatcher.Filter = "*";
        _fileSystemWatcher.IncludeSubdirectories = true;
        _fileSystemWatcher.EnableRaisingEvents = enableChangeEvent;
    }

    protected override void OnError(ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger.LogError(ex, $"An error occurred while watching changes of directory \"{Directory.FullName}\": {ex.Message}");
    }


    protected override void OnChange(FileSystemEventArgs e)
    {
        if (DirectoryChangedEventEnabled)
            DirectoryChanged?.Invoke(this, new DirectoryChangedEventHandlerArgs(Directory, e));
    }


}