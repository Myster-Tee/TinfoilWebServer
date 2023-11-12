using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class TinfoilIndexBuilder : ITinfoilIndexBuilder
{
    private readonly IAppSettings _appSettings;
    private readonly ILogger<TinfoilIndexBuilder> _logger;

    public TinfoilIndexBuilder(IAppSettings appSettings, ILogger<TinfoilIndexBuilder> logger)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings.PropertyChanged += OnAppSettingsChanged;
    }

    private void OnAppSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppSettings.CustomIndexPath))
        {
            _logger.LogInformation("Custom index updated.");
        }
    }

    public JsonObject Build(VirtualDirectory virtualDirectory, string? userMessageOfTheDay)
    {

        JsonObject indexJsonObject;

        using var fileStream = File.Open(_appSettings.CustomIndexPath, FileMode.Open);
        var jsonNode = JsonNode.Parse(fileStream);
        if (jsonNode is JsonObject jsonObject)
        {
            indexJsonObject = jsonObject;
        }
        else
        {
            indexJsonObject = new JsonObject();
        }

        JsonArray jsonFiles;
        if (indexJsonObject.TryGetPropertyValue("files", out var filesJsonNode))
        {
            if (filesJsonNode is JsonArray jsonArray)
                jsonFiles = jsonArray;
            else
            {
                // TODO: loguer un message
                jsonFiles = new JsonArray();
                indexJsonObject["files"] = jsonFiles;
            }
        }
        else
        {
            jsonFiles = new JsonArray();
            indexJsonObject.Add("files", jsonFiles);
        }

        foreach (var vf in virtualDirectory.GetDescendantFiles())
        {
            jsonFiles.Add(new JsonObject
            {
                {"url", JsonValue.Create( vf.BuildRelativeUrl(virtualDirectory))},
                {"size", JsonValue.Create(vf.Size)}
            });
        }

        if (userMessageOfTheDay != null)
        {
            indexJsonObject["success"] = userMessageOfTheDay;
        }


        return indexJsonObject;
    }

}