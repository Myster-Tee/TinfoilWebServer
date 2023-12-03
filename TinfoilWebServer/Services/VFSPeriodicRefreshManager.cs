using System;
using System.ComponentModel;
using System.Timers;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class VFSPeriodicRefreshManager : IVFSPeriodicRefreshManager, IDisposable
{
    private readonly IVirtualFileSystemRootProvider _virtualFileSystemRootProvider;
    private readonly ICacheSettings _cacheSettings;
    private readonly ILogger<VFSPeriodicRefreshManager> _logger;
    private readonly Timer _timer = new();


    public VFSPeriodicRefreshManager(IVirtualFileSystemRootProvider virtualFileSystemRootProvider, ICacheSettings cacheSettings, ILogger<VFSPeriodicRefreshManager> logger)
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
        if (_cacheSettings.PeriodicRefreshDelay != null)
        {
            SafeEnable(_cacheSettings.PeriodicRefreshDelay.Value);
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
            _logger.LogError(ex, $"Failed to start periodic refresh of served files cache with value {delay}: {ex.Message}");
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
            _logger.LogError(ex, $"Failed to stop periodic refresh served files cache: {ex.Message}");
        }
    }

    private void OnCacheSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ICacheSettings.PeriodicRefreshDelay))
        {
            var periodicRefreshDelay = _cacheSettings.PeriodicRefreshDelay;
            if (periodicRefreshDelay == null)
            {
                _logger.LogInformation("Disabling periodic refresh of served files cache.");
                SafeDisable();
            }
            else
            {
                _logger.LogInformation($"Enabling periodic refresh of served files cache every {periodicRefreshDelay.Value}.");
                SafeEnable(periodicRefreshDelay.Value);
            }
        }
    }

    private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _logger.LogDebug($"Served files cache invoked from {this.GetType().Name}.");

        await _virtualFileSystemRootProvider.SafeRefresh();
        try
        {
            _timer.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to rearm periodic refresh of served files cache: {ex.Message}");
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