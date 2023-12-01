using System;
using System.IO;
using System.Timers;
using Microsoft.Extensions.Logging;

namespace TinfoilWebServer.Services.FSChangeDetection;

/// <summary>
/// Base class for watching FileSystem changes with filtering
/// to avoid triggering time consuming operations on each FileSystem change
/// when changes occur too fast.
/// </summary>
public abstract class WatchedPathFiltered : IDisposable
{
    private readonly ILogger<WatchedPathFiltered> _logger;
    protected readonly FileSystemWatcher _fileSystemWatcher = new();
    private readonly Timer _timer = new();
    private FileSystemEventArgs? _lastChange;

    protected WatchedPathFiltered(ILogger<WatchedPathFiltered> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileSystemWatcher.Changed += OnFSWChanged;
        _fileSystemWatcher.Created += OnFSWCreated;
        _fileSystemWatcher.Deleted += OnFSWDeleted;
        _fileSystemWatcher.Renamed += OnFSWRenamed;
        _fileSystemWatcher.Error += OnFSWError;

        _timer.AutoReset = false;
        _timer.Interval = 2000;
        _timer.Elapsed += OnFilterTimerElapsed;
    }

    /// <summary>
    /// The filtering time. The time to wait without having any other FileSystem change, in order to call the <see cref="OnChange"/> method. 
    /// </summary>
    public TimeSpan FilterTime
    {
        get => TimeSpan.FromMilliseconds(_timer.Interval);
        set => _timer.Interval = value.TotalMicroseconds;
    }

    private void OnFSWRenamed(object sender, RenamedEventArgs e)
    {
        _logger.LogDebug($"{nameof(OnFSWRenamed)}: {e.OldFullPath} -> {e.FullPath}.");
        _timer.Stop();
        _timer.Start();
        _lastChange = e;
    }

    private void OnFSWDeleted(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug($"{nameof(OnFSWDeleted)}: {e.FullPath}");
        _timer.Stop();
        _timer.Start();
        _lastChange = e;
    }

    private void OnFSWCreated(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug($"{nameof(OnFSWCreated)}: {e.FullPath}");
        _timer.Stop();
        _timer.Start();
        _lastChange = e;
    }

    private void OnFSWChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug($"{nameof(OnFSWChanged)}: {e.FullPath}");
        _timer.Stop();
        _timer.Start();
        _lastChange = e;
    }

    private void OnFSWError(object sender, ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger.LogDebug(ex, $"{nameof(OnFSWError)}: {ex}");
        OnError(e);
    }

    private void OnFilterTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        OnChange(_lastChange!);
    }

    protected abstract void OnError(ErrorEventArgs e);

    protected abstract void OnChange(FileSystemEventArgs e);

    public void Dispose()
    {
        try
        {
            _fileSystemWatcher.Changed -= OnFSWChanged;
            _fileSystemWatcher.Created -= OnFSWCreated;
            _fileSystemWatcher.Deleted -= OnFSWDeleted;
            _fileSystemWatcher.Renamed -= OnFSWRenamed;
            _fileSystemWatcher.Error -= OnFSWError;
            _fileSystemWatcher.Dispose();
        }
        catch
        {
            //ignore
        }

        try
        {
            _timer.Dispose();
        }
        catch
        {
            //ignore
        }
    }
}