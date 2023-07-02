namespace TinfoilWebServer.Services;


/// <summary>
/// Helper for tracking all changes of a file
/// </summary>
public interface IFileChangeHelper
{

    /// <summary>
    /// Triggered when the watched file needs to be reloaded or when the file doesn't exist anymore
    /// </summary>
    public event FileChangedEventHandler FileChanged;

    /// <summary>
    /// True to enable <see cref="FileChanged"/> events, false otherwise
    /// </summary>
    public bool EnableFileChangedEvent { get; set; }

    /// <summary>
    /// The full path of the watched file (<see cref="WatchFile"/>)
    /// </summary>
    public string? WatchedFileFullPath { get; }

    /// <summary>
    /// Starts to watch file changes.
    /// The directory of the specified file should exist
    /// </summary>
    /// <param name="fullFilePath">The rooted path of the file to watch</param>
    /// <param name="enableFileChangedEvent"></param>
    void WatchFile(string fullFilePath, bool enableFileChangedEvent);

    /// <summary>
    /// Stops watching the file
    /// </summary>
    /// <returns></returns>
    bool StopWatching();

}


public delegate void FileChangedEventHandler(object sender, FileChangedEventHandlerArgs args);

public class FileChangedEventHandlerArgs
{
    public FileChangedEventHandlerArgs(string watchedFileFullPath)
    {
        WatchedFileFullPath = watchedFileFullPath;
    }

    public string WatchedFileFullPath { get; private set; }
}