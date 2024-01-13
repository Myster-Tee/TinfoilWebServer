using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class VirtualFileSystemRootProvider : IVirtualFileSystemRootProvider
{

    private readonly IVirtualFileSystemBuilder _virtualFileSystemBuilder;
    private readonly IAppSettings _appSettings;
    private readonly ILogger<VirtualFileSystemRootProvider> _logger;

    public VirtualFileSystemRootProvider(IVirtualFileSystemBuilder virtualFileSystemBuilder, IAppSettings appSettings, ILogger<VirtualFileSystemRootProvider> logger)
    {
        _virtualFileSystemBuilder = virtualFileSystemBuilder ?? throw new ArgumentNullException(nameof(virtualFileSystemBuilder));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _appSettings.PropertyChanged += OnAppSettingsChanged;
    }

    public VirtualFileSystemRoot Root { get; private set; } = new();

    private void OnAppSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppSettings.StripDirectoryNames))
        {
            _ = SafeRefresh();
        }
        else if (e.PropertyName == nameof(IAppSettings.ServeEmptyDirectories))
        {
            _ = SafeRefresh();
        }
        else if (e.PropertyName == nameof(IAppSettings.ServedDirectories))
        {
            _ = SafeRefresh();
        }
        else if (e.PropertyName == nameof(IAppSettings.AllowedExt))
        {
            _ = SafeRefresh();
        }
    }

    public async Task SafeRefresh()
    {
        try
        {
            _logger.LogInformation($"Refreshing served files cache.");

            var dateTime = DateTime.Now;
            var root = await UpdateVirtualFileSystem(_appSettings.ServedDirectories);
            Root = root;

            var nbFilesServed = root.GetDescendantFiles().Count();

            _logger.LogInformation($"Served files cache refreshed in {(DateTime.Now - dateTime).TotalSeconds:0.00}s, {nbFilesServed} file(s) served.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to refresh cache of served files: {ex.Message}");
        }
    }

    private Task<VirtualFileSystemRoot> UpdateVirtualFileSystem(IReadOnlyList<DirectoryInfo> servedDirectories)
    {
        return Task.Run(() =>
        {
            var root = _appSettings.StripDirectoryNames
                ? _virtualFileSystemBuilder.BuildFlat(servedDirectories)
                : _virtualFileSystemBuilder.BuildHierarchical(servedDirectories, !_appSettings.ServeEmptyDirectories);

            return root;
        });
    }
}

