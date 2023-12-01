using System;
using System.IO;

namespace TinfoilWebServer.Services.FSChangeDetection;


/// <summary>
/// Helper for tracking all changes of a file for a given path
/// </summary>
public interface IFileChangeHelper
{

    /// <summary>
    /// Starts to watch file changes.
    /// The directory of the specified file should exist
    /// </summary>
    /// <param name="filePath">The path of the file to watch</param>
    /// <param name="enableChangeEvent"></param>
    IWatchedFile WatchFile(string filePath, bool enableChangeEvent = true);

}

public interface IWatchedFile : IDisposable
{
    /// <summary>
    /// Triggered when the watched file needs to be reloaded or when the file doesn't exist anymore
    /// </summary>
    public event FileChangedEventHandler FileChanged;

    /// <summary>
    /// True to enable <see cref="FileChanged"/> events, false otherwise
    /// </summary>
    public bool FileChangedEventEnabled { get; set; }

    /// <summary>
    /// The path of the watched file
    /// </summary>
    public string WatchedFilePath { get; }

}


public delegate void FileChangedEventHandler(object sender, FileChangedEventHandlerArgs args);

public class FileChangedEventHandlerArgs
{
    public FileSystemEventArgs SystemEventArgs { get; }

    public string WatchedFilePath { get; private set; }

    public FileChangedEventHandlerArgs(string watchedFilePath, FileSystemEventArgs fileSystemEventArgs)
    {
        SystemEventArgs = fileSystemEventArgs ?? throw new ArgumentNullException(nameof(fileSystemEventArgs));
        WatchedFilePath = watchedFilePath ?? throw new ArgumentNullException(nameof(watchedFilePath));
    }
}