using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using TinfoilWebServer.Booting;
using TinfoilWebServer.Services.FSChangeDetection;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class VirtualFileSystemRootProvider : IVirtualFileSystemRootProvider
{

    private readonly Dictionary<string, IWatchedDirectory> _watchedDirectoriesPerFullPath = new();


    private readonly IVirtualFileSystemBuilder _virtualFileSystemBuilder;
    private readonly IAppSettings _appSettings;
    private readonly ILogger<VirtualFileSystemRootProvider> _logger;
    private readonly IBootInfo _bootInfo;
    private readonly IDirectoryChangeHelper _directoryChangeHelper;

    public VirtualFileSystemRootProvider(IVirtualFileSystemBuilder virtualFileSystemBuilder, IAppSettings appSettings, ILogger<VirtualFileSystemRootProvider> logger, IBootInfo bootInfo, IDirectoryChangeHelper directoryChangeHelper)
    {
        _virtualFileSystemBuilder = virtualFileSystemBuilder ?? throw new ArgumentNullException(nameof(virtualFileSystemBuilder));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bootInfo = bootInfo ?? throw new ArgumentNullException(nameof(bootInfo));
        _directoryChangeHelper = directoryChangeHelper ?? throw new ArgumentNullException(nameof(directoryChangeHelper));

        _appSettings.PropertyChanged += OnAppSettingsChanged;
    }

    public void Refresh()
    {
        SafeRefreshInternal(true);
    }

    private void SafeRefreshInternal(bool refreshWatchedDirectories)
    {
        try
        {
            var servedDirectoryPaths = _appSettings.ServedDirectories;
            if (servedDirectoryPaths.Count <= 0)
                _logger.LogWarning($"No served directory defined in configuration file \"{_bootInfo.ConfigFileFullPath}\".");

            var servedDirectories = new List<DirectoryInfo>();

            foreach (var servedDirectoryPath in servedDirectoryPaths)
            {
                if (string.IsNullOrWhiteSpace(servedDirectoryPath))
                {
                    _logger.LogError("Invalid configuration, served directory path can't be empty.");
                    continue;
                }

                var servedDirectory = new DirectoryInfo(servedDirectoryPath);
                if (!servedDirectory.Exists)
                {
                    _logger.LogError($"Served directory \"{servedDirectoryPath}\" doesn't exist.");
                    continue;
                }

                servedDirectories.Add(servedDirectory);
            }

            if (refreshWatchedDirectories)
            {
                RefreshWatchedDirectories(servedDirectories);
            }

            Root = UpdateVirtualFileSystem(servedDirectories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to refresh cache of served files: {ex.Message}");
        }

    }

    private void RefreshWatchedDirectories(IReadOnlyList<DirectoryInfo> servedDirectories)
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
                    _logger.LogError(ex, $"Failed to initialize changes detection of served directory \"{servedDirectory}\": {ex.Message}");
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
                _logger.LogInformation($"Changes detection of previously served directory \"{removedDirectory}\" removed.");
            }
        }
    }

    private void OnDirectoryChanged(object sender, DirectoryChangedEventHandlerArgs args)
    {
        SafeRefreshInternal(true);
    }

    private void OnAppSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppSettings.StripDirectoryNames))
        {
            _logger.LogInformation($"Setting \"{nameof(IAppSettings.StripDirectoryNames)}\" changed, refreshing served files cache.");
            SafeRefreshInternal(false);
        }
        else if (e.PropertyName == nameof(IAppSettings.ServeEmptyDirectories))
        {
            _logger.LogInformation($"Setting \"{nameof(IAppSettings.ServeEmptyDirectories)}\" changed, refreshing served files cache.");
            SafeRefreshInternal(false);
        }
        else if (e.PropertyName == nameof(IAppSettings.ServedDirectories))
        {
            _logger.LogInformation($"Setting \"{nameof(IAppSettings.ServedDirectories)}\" changed, refreshing served files cache.");
            SafeRefreshInternal(true);
        }
    }


    public VirtualFileSystemRoot Root { get; private set; } = new();


    private VirtualFileSystemRoot UpdateVirtualFileSystem(IReadOnlyList<DirectoryInfo> servedDirectories)
    {
        var dateTime = DateTime.Now;

        var root = _appSettings.StripDirectoryNames ?
            _virtualFileSystemBuilder.BuildFlat(servedDirectories) :
            _virtualFileSystemBuilder.BuildHierarchical(servedDirectories, !_appSettings.ServeEmptyDirectories);

        var nbFilesServed = root.GetDescendantFiles().Count();
        _logger.LogInformation($"Served files cache updated in {(DateTime.Now - dateTime).TotalSeconds:0.00}s, {nbFilesServed} file(s) served.");

        return root;
    }
}

