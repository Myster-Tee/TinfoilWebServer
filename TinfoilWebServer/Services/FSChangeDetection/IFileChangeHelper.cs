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
    /// <param name="file">The file to watch</param>
    /// <param name="enableChangeEvent"></param>
    IWatchedFile WatchFile(FileInfo file, bool enableChangeEvent = true);

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
    /// The watched file
    /// </summary>
    public FileInfo File { get; }

}


public delegate void FileChangedEventHandler(object sender, FileChangedEventHandlerArgs args);

public class FileChangedEventHandlerArgs
{
    public FileSystemEventArgs SystemEventArgs { get; }

    public FileInfo WatchedFile { get; }

    public FileChangedEventHandlerArgs(FileInfo watchedFile, FileSystemEventArgs fileSystemEventArgs)
    {
        SystemEventArgs = fileSystemEventArgs ?? throw new ArgumentNullException(nameof(fileSystemEventArgs));
        WatchedFile = watchedFile ?? throw new ArgumentNullException(nameof(watchedFile));
    }
}