using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Services.FSChangeDetection;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class VFSAutoRefreshManager : IVFSAutoRefreshManager
{
    private readonly IVirtualFileSystemRootProvider _virtualFileSystemRootProvider;
    private readonly IAppSettings _appSettings;
    private readonly ICacheSettings _cacheSettings;
    private readonly IDirectoryChangeHelper _directoryChangeHelper;
    private readonly ILogger<VFSAutoRefreshManager> _logger;

    private readonly Dictionary<string, IWatchedDirectory> _watchedDirectoriesPerFullPath = new();

    public VFSAutoRefreshManager(IVirtualFileSystemRootProvider virtualFileSystemRootProvider, IAppSettings appSettings, ICacheSettings cacheSettings, IDirectoryChangeHelper directoryChangeHelper, ILogger<VFSAutoRefreshManager> logger)
    {
        _virtualFileSystemRootProvider = virtualFileSystemRootProvider ?? throw new ArgumentNullException(nameof(virtualFileSystemRootProvider));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _cacheSettings = cacheSettings ?? throw new ArgumentNullException(nameof(cacheSettings));
        _directoryChangeHelper = directoryChangeHelper ?? throw new ArgumentNullException(nameof(directoryChangeHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _cacheSettings.PropertyChanged += OnCacheSettingsChanged;
        _appSettings.PropertyChanged += OnAppSettingsChanged;
    }

    private void OnAppSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppSettings.ServedDirectories))
        {
            _logger.LogInformation("Refreshing file system change detection of served directories.");
            SafeRefreshWatchedDirectories(_appSettings.ServedDirectories);
        }
    }

    private void OnCacheSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ICacheSettings.AutoDetectChanges))
        {
            if (_cacheSettings.AutoDetectChanges)
            {
                _logger.LogInformation("Enabling automatic refresh of served files cache.");
                SafeRefreshWatchedDirectories(_appSettings.ServedDirectories);
            }
            else
            {
                _logger.LogInformation("Disabling automatic refresh of served files cache.");
                SafeRefreshWatchedDirectories(Array.Empty<DirectoryInfo>());
            }
        }
    }

    private void SafeRefreshWatchedDirectories(IReadOnlyList<DirectoryInfo> servedDirectories)
    {
        try
        {
            lock (_watchedDirectoriesPerFullPath)
            {
                foreach (var servedDirectory in servedDirectories)
                {
                    if (_watchedDirectoriesPerFullPath.ContainsKey(servedDirectory.FullName))
                        continue;

                    IWatchedDirectory watchedDirectory;
                    try
                    {
                        watchedDirectory = _directoryChangeHelper.WatchDirectory(servedDirectory);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to initialize file system change detection of served directory \"{servedDirectory}\": {ex.Message}");
                        continue;
                    }

                    watchedDirectory.DirectoryChanged += OnDirectoryChanged;
                    _watchedDirectoriesPerFullPath.Add(servedDirectory.FullName, watchedDirectory);
                }

                var removedDirectories = _watchedDirectoriesPerFullPath.Keys.ToArray().Where(fullPath => !servedDirectories.Select(directory => directory.FullName).Contains(fullPath));

                foreach (var removedDirectory in removedDirectories)
                {
                    var watchedDirectory = _watchedDirectoriesPerFullPath[removedDirectory];
                    watchedDirectory.DirectoryChanged -= OnDirectoryChanged;
                    watchedDirectory.Dispose();

                    _watchedDirectoriesPerFullPath.Remove(removedDirectory);
                    _logger.LogInformation($"File system change detection of previously served directory \"{removedDirectory}\" stopped.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to refresh file system change detection of served directories: {ex.Message}");
        }
    }

    private async void OnDirectoryChanged(object sender, DirectoryChangedEventHandlerArgs args)
    {
        _logger.LogDebug($"Served files cache invoked from {this.GetType().Name}.");

        await _virtualFileSystemRootProvider.SafeRefresh();
    }

    public void Initialize()
    {
        if (_cacheSettings.AutoDetectChanges)
            SafeRefreshWatchedDirectories(_appSettings.ServedDirectories);
    }
}
