using System;
using System.ComponentModel;
using System.Timers;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class VFSForcedRefreshManager : IVFSForcedRefreshManager, IDisposable
{
    private readonly IVirtualFileSystemRootProvider _virtualFileSystemRootProvider;
    private readonly ICacheSettings _cacheSettings;
    private readonly ILogger<VFSForcedRefreshManager> _logger;
    private readonly Timer _timer = new();


    public VFSForcedRefreshManager(IVirtualFileSystemRootProvider virtualFileSystemRootProvider, ICacheSettings cacheSettings, ILogger<VFSForcedRefreshManager> logger)
    {
        _virtualFileSystemRootProvider = virtualFileSystemRootProvider ?? throw new ArgumentNullException(nameof(virtualFileSystemRootProvider));
        _cacheSettings = cacheSettings ?? throw new ArgumentNullException(nameof(cacheSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _cacheSettings.PropertyChanged += OnCacheSettingsChanged;
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = false;
    }

    public void Initialize()
    {
        if (_cacheSettings.ForcedRefreshDelay != null)
        {
            SafeEnable(_cacheSettings.ForcedRefreshDelay.Value);
        }
    }

    private void SafeEnable(TimeSpan delay)
    {
        try
        {
            _timer.Interval = delay.TotalMilliseconds;
            _timer.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to start automatic refresh delay of served directories with value {delay}: {ex.Message}");
        }
    }

    private void SafeDisable()
    {
        try
        {
            _timer.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to stop automatic refresh delay of served directories: {ex.Message}");
        }
    }

    private void OnCacheSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ICacheSettings.ForcedRefreshDelay))
        {
            var forcedRefreshDelay = _cacheSettings.ForcedRefreshDelay;
            if (forcedRefreshDelay == null)
            {
                _logger.LogInformation("Disabling automatic refresh delay of served directories.");
                SafeDisable();
            }
            else
            {
                _logger.LogInformation($"Enabling automatic refresh delay of served directories every {forcedRefreshDelay.Value}.");
                SafeEnable(forcedRefreshDelay.Value);
            }
        }
    }

    private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        await _virtualFileSystemRootProvider.Refresh();
        try
        {
            _timer.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to restart automatic refresh delay of served directories: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            _timer.Elapsed -= OnTimerElapsed;
            _timer.Dispose();
        }
        catch
        {
            // ignored
        }
    }

}