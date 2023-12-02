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

    public FileInfo File { get; }

    public WatchedFile(FileInfo file, bool enableChangeEvent, ILogger<WatchedFile> logger) : base(logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        File = file ?? throw new ArgumentNullException(nameof(file));


        var fileDirectory =  file.Directory;
        if (fileDirectory == null)
            throw new ArgumentException($@"The directory of file to watch ""{file}"" can't be determined.", nameof(file));

        _fileSystemWatcher.Path = fileDirectory.FullName;
        _fileSystemWatcher.Filter = file.Name;
        _fileSystemWatcher.IncludeSubdirectories = false;
        _fileSystemWatcher.EnableRaisingEvents = enableChangeEvent;
    }

    protected override void OnError(ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger.LogError(ex, $"An error occurred while watching changes of file \"{File}\": {ex.Message}");
    }

    protected override void OnChange(FileSystemEventArgs e)
    {
        if (FileChangedEventEnabled)
            FileChanged?.Invoke(this, new FileChangedEventHandlerArgs(File, e));
    }


}