using System;
using System.IO;

namespace TinfoilWebServer.Services.FSChangeDetection;

public interface IDirectoryChangeHelper
{
    IWatchedDirectory WatchDirectory(DirectoryInfo directory, bool enableChangeEvent = true);
}

public interface IWatchedDirectory : IDisposable
{
    /// <summary>
    /// Triggered when a changed occurred in the watched directory (renaming, deletion, creation, etc.)
    /// </summary>
    public event DirectoryChangedEventHandler DirectoryChanged;

    /// <summary>
    /// True to enable <see cref="DirectoryChanged"/> events, false otherwise
    /// </summary>
    public bool DirectoryChangedEventEnabled { get; set; }

    /// <summary>
    /// The watched directory
    /// </summary>
    public DirectoryInfo Directory { get; }
}

public delegate void DirectoryChangedEventHandler(object sender, DirectoryChangedEventHandlerArgs args);

public class DirectoryChangedEventHandlerArgs
{
    public FileSystemEventArgs SystemEventArgs { get; }

    public DirectoryInfo WatchedDirectory { get; private set; }

    public DirectoryChangedEventHandlerArgs(DirectoryInfo watchedDirectory, FileSystemEventArgs fileSystemEventArgs)
    {
        SystemEventArgs = fileSystemEventArgs ?? throw new ArgumentNullException(nameof(fileSystemEventArgs));
        WatchedDirectory = watchedDirectory ?? throw new ArgumentNullException(nameof(watchedDirectory));
    }
}

