using System;
using System.Linq;
using TinfoilWebServer.Models;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class TinfoilIndexBuilder : ITinfoilIndexBuilder
{
    private readonly IAppSettings _appSettings;

    public TinfoilIndexBuilder(IAppSettings appSettings)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
    }

    public TinfoilIndex Build(VirtualDirectory virtualDirectory)
    {
        var tinfoilIndex = new TinfoilIndex
        {
            Success = _appSettings.MessageOfTheDay,
            Files = virtualDirectory.GetDescendantFiles().Select(vf => new FileNfo { Size = vf.Size, Url = vf.BuildRelativeUrl(virtualDirectory) }).ToArray(),
            Directories = _appSettings.ExtraRepositories ?? Array.Empty<string>()
        };

        return tinfoilIndex;
    }

}