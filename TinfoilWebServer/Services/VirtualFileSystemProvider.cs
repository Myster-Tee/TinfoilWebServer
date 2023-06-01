using Microsoft.Extensions.Logging;
using System;
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
    }

    private bool IsCacheExpired
    {
        get
        {
            var cacheExpirationSettings = _appSettings.CacheExpiration;
            if (cacheExpirationSettings == null || !cacheExpirationSettings.Enabled)
                return false;

            if (_lastCacheCreationDate == null)
                return true;

            return DateTime.Now > _lastCacheCreationDate.Value + cacheExpirationSettings.ExpirationDelay;
        }
    }

    public VirtualFileSystemRoot Root => GetRoot();


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
        _logger.LogInformation($"Served files cache updated ({nbFilesServed} files served).");
        _lastCacheCreationDate = DateTime.Now;
    }
}