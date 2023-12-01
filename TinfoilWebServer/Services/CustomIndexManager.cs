using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Services.FSChangeDetection;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class CustomIndexManager : ICustomIndexManager
{
    /// <summary>
    /// Internal model in charge of aggregating cached data:
    /// - The path of a custom index file
    /// - The parsed <see cref="JsonObject"/> corresponding to the file via property <see cref="CustomIndex"/>
    /// - The <see cref="IWatchedFile"/> utility for tracking file changes in order to updated the parsed <see cref="JsonObject"/>
    /// </summary>
    private class CachedData : IDisposable
    {
        private readonly ILogger<CustomIndexManager> _logger;

        public CachedData(string customIndexPath, IWatchedFile? watchedFile, ILogger<CustomIndexManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CustomIndexPath = customIndexPath ?? throw new ArgumentNullException(nameof(customIndexPath));
            WatchedFile = watchedFile;

            if (watchedFile != null)
                watchedFile.FileChanged += OnFileChanged;
        }

        private void OnFileChanged(object sender, FileChangedEventHandlerArgs args)
        {
            RefreshSafe();
        }

        public string CustomIndexPath { get; }

        public IWatchedFile? WatchedFile { get; }

        public JsonObject? CustomIndex { get; private set; }


        public void RefreshSafe()
        {
            try
            {
                if (!File.Exists(CustomIndexPath))
                {
                    _logger.LogError($"Custom index file \"{CustomIndexPath}\" not found.");
                    CustomIndex = null;
                    return;
                }

                using var fileStream = File.Open(CustomIndexPath, FileMode.Open);

                if (JsonNode.Parse(fileStream) is not JsonObject jsonObject)
                {
                    _logger.LogError($"Custom index file \"{CustomIndexPath}\" is not a valid JSON object.");
                }
                else
                {
                    _logger.LogInformation($"Custom index file \"{CustomIndexPath}\" successfully loaded.");
                    CustomIndex = jsonObject;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load custom index from file \"{CustomIndexPath}\": {ex.Message}");
            }
        }

        public void Dispose()
        {
            WatchedFile?.Dispose();
        }
    }

    private readonly IAuthenticationSettings _authenticationSettings;
    private readonly IAppSettings _appSettings;
    private readonly IFileChangeHelper _fileChangeHelper;
    private readonly ILogger<CustomIndexManager> _logger;
    private readonly Dictionary<string, CachedData> _cachedDataPerPath = new();

    public CustomIndexManager(IAuthenticationSettings authenticationSettings, IAppSettings appSettings, IFileChangeHelper fileChangeHelper, ILogger<CustomIndexManager> logger)
    {
        _authenticationSettings = authenticationSettings ?? throw new ArgumentNullException(nameof(authenticationSettings));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _fileChangeHelper = fileChangeHelper ?? throw new ArgumentNullException(nameof(fileChangeHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RefreshCustomIndexesCache();

        authenticationSettings.PropertyChanged += OnAuthenticationSettingsChanged;
        appSettings.PropertyChanged += OnAppSettingsChanged;
    }


    private void OnAppSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppSettings.CustomIndexPath))
        {
            RefreshCustomIndexesCache();
        }
    }

    private void OnAuthenticationSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAuthenticationSettings.Users))
        {
            RefreshCustomIndexesCache();
        }
    }

    private void RefreshCustomIndexesCache()
    {
        List<string> newCustomIndexPaths = _authenticationSettings.Users
            .Select(user => user.CustomIndexPath)
            .Append(_appSettings.CustomIndexPath) // Adds also the global custom index
            .Where(cip => !string.IsNullOrWhiteSpace(cip))
            .ToList()!;

        lock (_cachedDataPerPath)
        {
            foreach (var newCustomIndexPath in newCustomIndexPaths)
            {
                if (_cachedDataPerPath.ContainsKey(newCustomIndexPath))
                    continue; // Already loaded in the set

                IWatchedFile? watchedFile = null;
                try
                {
                    watchedFile = _fileChangeHelper.WatchFile(newCustomIndexPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to watch changes of custom index file \"{newCustomIndexPath}\": {ex.Message}");
                }

                var cachedData = new CachedData(newCustomIndexPath, watchedFile, _logger);
                cachedData.RefreshSafe();

                _cachedDataPerPath.Add(newCustomIndexPath, cachedData);
            }

            // Removes extra custom index from cache 
            foreach (var cachedCustomIndexPath in _cachedDataPerPath.Keys.ToArray())
            {
                if (!newCustomIndexPaths.Contains(cachedCustomIndexPath))
                {
                    // Custom index path is not anymore referenced and can be removed from cache
                    _cachedDataPerPath[cachedCustomIndexPath].Dispose();
                    _cachedDataPerPath.Remove(cachedCustomIndexPath);
                    _logger.LogInformation($"Custom index file \"{cachedCustomIndexPath}\" unloaded.");
                }
            }
        }
    }

    public JsonObject? GetCustomIndex(string? customIndexPath)
    {
        if (string.IsNullOrWhiteSpace(customIndexPath))
            return null;

        if (!_cachedDataPerPath.TryGetValue(customIndexPath, out var cachedData))
            return null;

        return cachedData.CustomIndex;
    }

}