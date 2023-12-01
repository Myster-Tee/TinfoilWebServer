using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TinfoilWebServer.Booting;
using TinfoilWebServer.Services.FSChangeDetection;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class VirtualFileSystemRootProvider : IVirtualFileSystemRootProvider
{

    private readonly Dictionary<string, IWatchedDirectory> _watchedDirectoriesPerPath = new();


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
        RefreshInternal(true);
    }

    private void RefreshInternal(bool refreshWatchedDirectories)
    {
        var servedDirectories = _appSettings.ServedDirectories;

        if (refreshWatchedDirectories)
        {
            lock (_watchedDirectoriesPerPath)
            {

                foreach (var servedDirectory in servedDirectories)
                {
                    if (_watchedDirectoriesPerPath.ContainsKey(servedDirectory))
                        continue;

                    IWatchedDirectory watchedDirectory;
                    try
                    {
                        watchedDirectory = _directoryChangeHelper.WatchDirectory(servedDirectory);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to watch changes of served directory \"{servedDirectory}\": {ex.Message}");
                        continue;
                    }

                    watchedDirectory.DirectoryChanged += OnDirectoryChanged;
                    _watchedDirectoriesPerPath.Add(servedDirectory, watchedDirectory);
                }

                foreach (var oldWatchedDirectoryPath in _watchedDirectoriesPerPath.Keys.ToArray()
                             .Where(p => !servedDirectories.Contains(p)))
                {
                    var watchedDirectory = _watchedDirectoriesPerPath[oldWatchedDirectoryPath];
                    watchedDirectory.DirectoryChanged -= OnDirectoryChanged;
                    watchedDirectory.Dispose();

                    _watchedDirectoriesPerPath.Remove(oldWatchedDirectoryPath);
                    _logger.LogInformation($"Served directory \"{oldWatchedDirectoryPath}\" removed.");
                }

            }
        }

        try
        {
            Root = UpdateVirtualFileSystem(servedDirectories);
        }
        catch (Exception ex)
        {
            Root = new VirtualFileSystemRoot();
            _logger.LogError(ex, $"Failed to build cache of served files: {ex.Message}");
        }

    }

    private void OnDirectoryChanged(object sender, DirectoryChangedEventHandlerArgs args)
    {
        RefreshInternal(true);
    }

    private void OnAppSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppSettings.StripDirectoryNames))
        {
            _logger.LogInformation($"Setting \"{nameof(IAppSettings.StripDirectoryNames)}\" changed, refreshing served files cache.");
            RefreshInternal(false);
        }
        else if (e.PropertyName == nameof(IAppSettings.ServeEmptyDirectories))
        {
            _logger.LogInformation($"Setting \"{nameof(IAppSettings.ServeEmptyDirectories)}\" changed, refreshing served files cache.");
            RefreshInternal(false);
        }
        else if (e.PropertyName == nameof(IAppSettings.ServedDirectories))
        {
            _logger.LogInformation($"Setting \"{nameof(IAppSettings.ServedDirectories)}\" changed, refreshing served files cache.");
            RefreshInternal(true);
        }
    }


    public VirtualFileSystemRoot Root { get; private set; } = new();


    private VirtualFileSystemRoot UpdateVirtualFileSystem(IReadOnlyList<string> servedDirectories)
    {
        if (servedDirectories.Count <= 0)
            _logger.LogWarning($"No served directory defined in configuration file \"{_bootInfo.ConfigFileFullPath}\".");

        var dateTime = DateTime.Now;

        var root = _appSettings.StripDirectoryNames ?
            _virtualFileSystemBuilder.BuildFlat(servedDirectories) :
            _virtualFileSystemBuilder.BuildHierarchical(servedDirectories, !_appSettings.ServeEmptyDirectories);

        var nbFilesServed = root.GetDescendantFiles().Count();
        _logger.LogInformation($"Served files cache updated in {(DateTime.Now - dateTime).TotalSeconds:0.00}s, {nbFilesServed} file(s) served.");

        return root;
    }
}

