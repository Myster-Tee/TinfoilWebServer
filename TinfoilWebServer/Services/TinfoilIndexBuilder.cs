using System;
using System.ComponentModel;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Services.JSON;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class TinfoilIndexBuilder : ITinfoilIndexBuilder
{
    private readonly IAppSettings _appSettings;
    private readonly ICustomIndexManager _customIndexManager;
    private readonly IJsonMerger _jsonMerger;
    private readonly ILogger<TinfoilIndexBuilder> _logger;

    public TinfoilIndexBuilder(IAppSettings appSettings, ICustomIndexManager customIndexManager, IJsonMerger jsonMerger, ILogger<TinfoilIndexBuilder> logger)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _customIndexManager = customIndexManager;
        _jsonMerger = jsonMerger ?? throw new ArgumentNullException(nameof(jsonMerger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings.PropertyChanged += OnAppSettingsChanged;
    }

    private void OnAppSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppSettings.MessageOfTheDay))
        {
            _logger.LogInformation("Message of the day updated.");
        }
        else if (e.PropertyName == nameof(IAppSettings.CustomIndexPath))
        {
            _logger.LogInformation("Custom index updated.");
        }
    }

    public JsonObject Build(VirtualDirectory virtualDirectory, IUserInfo? user)
    {
        var jsonFiles = new JsonArray();
        foreach (var vf in virtualDirectory.GetDescendantFiles())
        {
            jsonFiles.Add(new JsonObject
            {
                { "url", JsonValue.Create( vf.BuildRelativeUrl(virtualDirectory)) },
                { "size", JsonValue.Create(vf.Size) }
            });
        }

        var baseIndex = new JsonObject
        {
            { "files", jsonFiles },
            { "success", user?.MessageOfTheDay ??  _appSettings.MessageOfTheDay }
        };

        var defaultCustomIndex = _customIndexManager.GetCustomIndex(_appSettings.CustomIndexPath);

        var userCustomIndex = _customIndexManager.GetCustomIndex(user?.CustomIndexPath);

        var mergedIndex = _jsonMerger.Merge(baseIndex, defaultCustomIndex, userCustomIndex);


        return mergedIndex;
    }

}