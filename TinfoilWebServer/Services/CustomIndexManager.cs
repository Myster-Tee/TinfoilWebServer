using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
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

        public CachedData(FileInfo customIndexFile, IWatchedFile? watchedFile, ILogger<CustomIndexManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CustomIndexFile = customIndexFile ?? throw new ArgumentNullException(nameof(customIndexFile));
            WatchedFile = watchedFile;

            if (watchedFile != null)
                watchedFile.FileChanged += OnFileChanged;
        }

        private void OnFileChanged(object sender, FileChangedEventHandlerArgs args)
        {
            RefreshSafe();
        }

        public FileInfo CustomIndexFile { get; }

        public IWatchedFile? WatchedFile { get; }

        public JsonObject? CustomIndex { get; private set; }


        public void RefreshSafe()
        {
            var customIndexFile = CustomIndexFile;
            try
            {
                customIndexFile.Refresh();
                if (!customIndexFile.Exists)
                {
                    _logger.LogError($"Custom index file \"{customIndexFile}\" not found.");
                    CustomIndex = null;
                    return;
                }

                using var fileStream = File.Open(customIndexFile.FullName, FileMode.Open);

                var jsonDocumentOptions = new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip
                };

                if (JsonNode.Parse(fileStream, null, jsonDocumentOptions) is not JsonObject jsonObject)
                {
                    _logger.LogError($"Custom index file \"{customIndexFile}\" is not a valid JSON object.");
                }
                else
                {
                    _logger.LogInformation($"Custom index file \"{customIndexFile}\" successfully loaded.");
                    CustomIndex = jsonObject;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load custom index from file \"{customIndexFile}\": {ex.Message}");
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
        var newCustomIndexes = _authenticationSettings.Users
            .Select(user => user.CustomIndexPath)
            .Append(_appSettings.CustomIndexPath) // Adds also the global custom index
            .Where(cip => !string.IsNullOrWhiteSpace(cip))
            .Select(cip => new
            {
                Path = cip!,
                File = new FileInfo(cip!)
            })
            .ToList();

        lock (_cachedDataPerPath)
        {
            // Add new custom index which are not already in the cache
            foreach (var customIndex in newCustomIndexes)
            {
                var customIndexPath = customIndex.Path;
                var customIndexFile = customIndex.File;

                if (_cachedDataPerPath.ContainsKey(customIndexPath))
                    continue; // Already loaded in the cache

                IWatchedFile? watchedFile = null;
                try
                {
                    watchedFile = _fileChangeHelper.WatchFile(customIndexFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to watch changes of custom index file \"{customIndexFile}\": {ex.Message}");
                }

                var cachedData = new CachedData(customIndexFile, watchedFile, _logger);
                cachedData.RefreshSafe();

                _cachedDataPerPath.Add(customIndexPath, cachedData);
            }

            // Removes extra custom index from cache
            foreach (var cachedCustomIndexPath in _cachedDataPerPath.Keys.ToArray())
            {
                if (newCustomIndexes.Select(customIndex => customIndex.Path).Contains(cachedCustomIndexPath))
                    continue;

                // Custom index path is not anymore referenced and can be removed from cache
                _cachedDataPerPath[cachedCustomIndexPath].Dispose();
                _cachedDataPerPath.Remove(cachedCustomIndexPath);
                _logger.LogInformation($"Custom index file \"{cachedCustomIndexPath}\" unloaded.");
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