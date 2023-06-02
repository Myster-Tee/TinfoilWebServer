using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Models;
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
        if (e.PropertyName == nameof(IAppSettings.MessageOfTheDay))
        {
            _logger.LogInformation("Message of the day updated.");
        }
        else if (e.PropertyName == nameof(IAppSettings.ExtraRepositories))
        {
            _logger.LogInformation("List of extra repositories updated.");
        }
    }

    public TinfoilIndex Build(VirtualDirectory virtualDirectory)
    {
        var tinfoilIndex = new TinfoilIndex
        {
            Success = _appSettings.MessageOfTheDay,
            Files = virtualDirectory.GetDescendantFiles().Select(vf => new FileNfo { Size = vf.Size, Url = vf.BuildRelativeUrl(virtualDirectory) }).ToArray(),
            Directories = _appSettings.ExtraRepositories
        };

        return tinfoilIndex;
    }

}