using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class VirtualFileSystemRootProvider : IVirtualFileSystemRootProvider
{
    private readonly IVirtualFileSystemBuilder _virtualFileSystemBuilder;
    private readonly IAppSettings _appSettings;
    private readonly ILogger<VirtualFileSystemRootProvider> _logger;
    private VirtualFileSystemRoot? _root;
    private DateTime? _lastCacheCreationDate;

    public VirtualFileSystemRootProvider(IVirtualFileSystemBuilder virtualFileSystemBuilder, IAppSettings appSettings, ILogger<VirtualFileSystemRootProvider> logger)
    {
        _virtualFileSystemBuilder = virtualFileSystemBuilder ?? throw new ArgumentNullException(nameof(virtualFileSystemBuilder));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _appSettings.PropertyChanged += OnAppSettingsChanged;
        _appSettings.CacheExpiration.PropertyChanged += OnCacheExpirationSettingsChanged;
    }

    private void OnCacheExpirationSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ICacheExpirationSettings.Enabled))
        {
            _lastCacheCreationDate = null;
            if (_appSettings.CacheExpiration.Enabled)
                _logger.LogInformation($"Cache expiration enabled.");
            else
                _logger.LogInformation($"Cache expiration disabled.");
        }
        else if (e.PropertyName == nameof(ICacheExpirationSettings.ExpirationDelay))
        {
            _logger.LogInformation($"Cache expiration delay changed to {_appSettings.CacheExpiration.ExpirationDelay}.");
        }

    }

    private void OnAppSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppSettings.StripDirectoryNames) ||
            e.PropertyName == nameof(IAppSettings.ServedDirectories) ||
            e.PropertyName == nameof(IAppSettings.ServeEmptyDirectories)
           )
        {
            _logger.LogInformation($"Served files cache needs to be updated due to configuration change.");

            UpdateVirtualFileSystem();
        }
    }


    private bool IsCacheExpired
    {
        get
        {
            var cacheExpirationSettings = _appSettings.CacheExpiration;
            if (!cacheExpirationSettings.Enabled)
                return false;

            if (_lastCacheCreationDate == null)
                return true;

            return DateTime.Now > _lastCacheCreationDate.Value + cacheExpirationSettings.ExpirationDelay;
        }
    }

    public VirtualFileSystemRoot Root => GetRoot();

    public void Initialize()
    {
        GetRoot();
    }

    private VirtualFileSystemRoot GetRoot()
    {
        if (_root == null || IsCacheExpired)
            UpdateVirtualFileSystem();

        return _root;
    }

    [MemberNotNull(nameof(_root))]
    private void UpdateVirtualFileSystem()
    {
        _root = _appSettings.StripDirectoryNames ?
            _virtualFileSystemBuilder.BuildFlat(_appSettings.ServedDirectories) :
            _virtualFileSystemBuilder.BuildHierarchical(_appSettings.ServedDirectories, !_appSettings.ServeEmptyDirectories);
        var nbFilesServed = _root.GetDescendantFiles().Count();
        _logger.LogInformation($"Served files cache updated, {nbFilesServed} file(s) served.");
        _lastCacheCreationDate = DateTime.Now;
    }
}