using System;
using System.IO;

namespace TinfoilWebServer.Services.FSChangeDetection;

public interface IDirectoryChangeHelper
{
    IWatchedDirectory WatchDirectory(string directoryPath, bool enableChangeEvent = true);
}

public interface IWatchedDirectory : IDisposable
{
    /// <summary>
    /// Triggered when the watched file needs to be reloaded or when the file doesn't exist anymore
    /// </summary>
    public event DirectoryChangedEventHandler DirectoryChanged;

    /// <summary>
    /// True to enable <see cref="DirectoryChanged"/> events, false otherwise
    /// </summary>
    public bool DirectoryChangedEventEnabled { get; set; }

    /// <summary>
    /// The path of the watched file
    /// </summary>
    public string WatchedDirectoryPath { get; }
}

public delegate void DirectoryChangedEventHandler(object sender, DirectoryChangedEventHandlerArgs args);

public class DirectoryChangedEventHandlerArgs
{
    public FileSystemEventArgs SystemEventArgs { get; }

    public string WatchedDirectoryPath { get; private set; }

    public DirectoryChangedEventHandlerArgs(string watchedDirectoryPath, FileSystemEventArgs fileSystemEventArgs)
    {
        SystemEventArgs = fileSystemEventArgs ?? throw new ArgumentNullException(nameof(fileSystemEventArgs));
        WatchedDirectoryPath = watchedDirectoryPath ?? throw new ArgumentNullException(nameof(watchedDirectoryPath));
    }
}

